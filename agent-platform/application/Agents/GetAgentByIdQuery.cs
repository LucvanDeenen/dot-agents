using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Agents;

public sealed record GetAgentByIdQuery(Guid Id) : IRequest<Agent?>;

public sealed class GetAgentByIdQueryHandler(IAgentDbContext db) : IRequestHandler<GetAgentByIdQuery, Agent?>
{
    public Task<Agent?> Handle(GetAgentByIdQuery request, CancellationToken cancellationToken) =>
        db.Agents
            .Include(a => a.Skills)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
}
