using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Skills;

public sealed record ListSkillsQuery : IRequest<List<Skill>>;

public sealed class ListSkillsQueryHandler(IAgentDbContext db) : IRequestHandler<ListSkillsQuery, List<Skill>>
{
    public Task<List<Skill>> Handle(ListSkillsQuery request, CancellationToken cancellationToken) =>
        db.Skills.OrderBy(s => s.Name).ToListAsync(cancellationToken);
}
