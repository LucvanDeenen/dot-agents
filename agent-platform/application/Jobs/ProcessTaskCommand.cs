using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentPlatform.Application.Jobs;

public sealed record ProcessTaskCommand(Guid TaskId) : IRequest;

// Invoked by TaskQueueListener once it dequeues a task message: resolves the
// agent (pinned, or matched by routing key), hands the pair to the runner and
// advances task status based on the outcome.
public sealed class ProcessTaskCommandHandler(
    IAgentDbContext db,
    IAgentRunner agentRunner,
    IJobStatusNotifier statusNotifier,
    ILogger<ProcessTaskCommandHandler> logger)
    : IRequestHandler<ProcessTaskCommand>
{
    public async Task Handle(ProcessTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await db.AgentTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found, skipping", request.TaskId);
            return;
        }

        if (task.Status is AgentTaskStatus.Completed or AgentTaskStatus.Failed)
            return;

        var agent = await ResolveAgentAsync(task, cancellationToken);
        if (agent is null)
        {
            task.Status = AgentTaskStatus.Failed;
            task.Output = $"No enabled agent matches routing key '{task.RoutingKey}'. " +
                          "Create an agent whose routing-key pattern covers it, or pin the task to an agent.";
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await statusNotifier.NotifyStatusChangedAsync(task, cancellationToken);
            return;
        }

        task.AgentId = agent.Id;
        task.Status = AgentTaskStatus.Running;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await statusNotifier.NotifyStatusChangedAsync(task, cancellationToken);

        try
        {
            var result = await agentRunner.RunAsync(task, agent, cancellationToken);

            task.Status = result.Succeeded ? AgentTaskStatus.Completed : AgentTaskStatus.Failed;
            task.Output = result.Output;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Task {TaskId} failed during processing", task.Id);
            task.Status = AgentTaskStatus.Failed;
            task.Output = ex.Message;
        }

        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await statusNotifier.NotifyStatusChangedAsync(task, cancellationToken);
    }

    private async Task<Agent?> ResolveAgentAsync(AgentTask task, CancellationToken cancellationToken)
    {
        if (task.AgentId is { } agentId)
        {
            var pinned = await db.Agents
                .Include(a => a.Skills)
                .FirstOrDefaultAsync(a => a.Id == agentId && a.Enabled, cancellationToken);
            if (pinned is not null) return pinned;
            logger.LogWarning("Pinned agent {AgentId} for task {TaskId} is missing or disabled; falling back to routing-key match",
                agentId, task.Id);
        }

        var candidates = await db.Agents
            .Include(a => a.Skills)
            .Where(a => a.Enabled)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(a => RoutingKeyMatcher.Matches(a.RoutingKeyPattern, task.RoutingKey));
    }
}
