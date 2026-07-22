using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Agents;

public sealed record ListAgentsQuery : IRequest<List<Agent>>;

public sealed class ListAgentsQueryHandler(IAgentDbContext db) : IRequestHandler<ListAgentsQuery, List<Agent>>
{
    public Task<List<Agent>> Handle(ListAgentsQuery request, CancellationToken cancellationToken) =>
        db.Agents
            .Include(a => a.Skills)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
}
