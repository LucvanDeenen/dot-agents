using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgentPlatform.Application.Jobs;

public sealed record CreateJobCommand(string Prompt, string? RepoUrl, string? Branch) : IRequest<AgentTask>;

public sealed class CreateJobCommandHandler(
    IAgentDbContext db,
    ITaskPublisher publisher,
    ILogger<CreateJobCommandHandler> logger) : IRequestHandler<CreateJobCommand, AgentTask>
{
    public async Task<AgentTask> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var task = new AgentTask
        {
            Instruction = request.Prompt,
            RoutingKey = "task.created",
            Status = AgentTaskStatus.Pending
        };

        db.AgentTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            // Routing key must match the binding pattern the topology
            // initializer declared (RabbitMqOptions.TaskRoutingKeyPattern).
            await publisher.PublishAsync(new TaskMessage(task.Id), task.RoutingKey, cancellationToken);
            task.Status = AgentTaskStatus.Queued;
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
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
}
