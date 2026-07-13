using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentPlatform.Application.Jobs;

public sealed record ProcessTaskCommand(Guid TaskId) : IRequest;

// Invoked by TaskQueueListener once it dequeues a task message. Runs the
// (currently placeholder) agent work and advances task status.
public sealed class ProcessTaskCommandHandler(IAgentDbContext db, ILogger<ProcessTaskCommandHandler> logger)
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

        task.Status = AgentTaskStatus.Running;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        task.Status = AgentTaskStatus.Completed;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
