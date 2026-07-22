using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Jobs;

public enum DeleteJobResult
{
    Deleted,
    NotFound,
    Running
}

public sealed record DeleteJobCommand(Guid Id) : IRequest<DeleteJobResult>;

public sealed class DeleteJobCommandHandler(IAgentDbContext db) : IRequestHandler<DeleteJobCommand, DeleteJobResult>
{
    public async Task<DeleteJobResult> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        var task = await db.AgentTasks.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (task is null) return DeleteJobResult.NotFound;

        // A running task has a live container whose completion will try to
        // update this row — let it finish (or fail) before cleaning up.
        if (task.Status is AgentTaskStatus.Running) return DeleteJobResult.Running;

        db.AgentTasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return DeleteJobResult.Deleted;
    }
}
