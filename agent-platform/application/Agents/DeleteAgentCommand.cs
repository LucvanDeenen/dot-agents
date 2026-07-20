using AgentPlatform.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Agents;

public sealed record DeleteAgentCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteAgentCommandHandler(IAgentDbContext db) : IRequestHandler<DeleteAgentCommand, bool>
{
    public async Task<bool> Handle(DeleteAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = await db.Agents.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (agent is null) return false;

        db.Agents.Remove(agent);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
