using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentPlatform.Application.Jobs;

public sealed record CreateJobCommand(
    string Prompt,
    string? RepoUrl,
    string? Branch,
    Guid? AgentId,
    string? RoutingKey) : IRequest<AgentTask>;

public sealed class CreateJobCommandHandler(
    IAgentDbContext db,
    ITaskPublisher publisher,
    IJobStatusNotifier statusNotifier,
    ILogger<CreateJobCommandHandler> logger) : IRequestHandler<CreateJobCommand, AgentTask>
{
    public async Task<AgentTask> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        Guid? agentId = null;
        if (request.AgentId is { } requestedAgentId)
        {
            var agentExists = await db.Agents.AnyAsync(a => a.Id == requestedAgentId, cancellationToken);
            if (agentExists)
                agentId = requestedAgentId;
            else
                logger.LogWarning("Requested agent {AgentId} does not exist; task will be dispatched by routing key", requestedAgentId);
        }

        var task = new AgentTask
        {
            Instruction = request.Prompt,
            RoutingKey = NormalizeRoutingKey(request.RoutingKey),
            Status = AgentTaskStatus.Pending,
            AgentId = agentId,
            RepoUrl = request.RepoUrl,
            Branch = request.Branch
        };

        db.AgentTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        await statusNotifier.NotifyStatusChangedAsync(task, cancellationToken);

        try
        {
            // Routing key must match the binding pattern the topology
            // initializer declared (RabbitMqOptions.TaskRoutingKeyPattern).
            await publisher.PublishAsync(new TaskMessage(task.Id), task.RoutingKey, cancellationToken);
            task.Status = AgentTaskStatus.Queued;
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await statusNotifier.NotifyStatusChangedAsync(task, cancellationToken);
        }
        catch (Exception ex)
        {
            // The task row exists either way — don't lose it because the broker
            // hiccuped. Leave it Pending; a reconciliation sweep or manual
            // republish can pick it up.
            logger.LogError(ex, "Failed to publish task {TaskId} to the task queue", task.Id);
            throw new TaskPublishFailedException(task, ex);
        }

        return task;
    }

    // The queue binding only matches task.* keys — force the prefix so a
    // custom key can't silently route into the void.
    private static string NormalizeRoutingKey(string? routingKey)
    {
        var key = routingKey?.Trim();
        if (string.IsNullOrEmpty(key)) return "task.created";
        return key.StartsWith("task.", StringComparison.Ordinal) ? key : $"task.{key}";
    }
}
