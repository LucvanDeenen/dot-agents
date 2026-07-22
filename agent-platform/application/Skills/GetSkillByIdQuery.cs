using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Skills;

public sealed record GetSkillByIdQuery(Guid Id) : IRequest<Skill?>;

public sealed class GetSkillByIdQueryHandler(IAgentDbContext db) : IRequestHandler<GetSkillByIdQuery, Skill?>
{
    public Task<Skill?> Handle(GetSkillByIdQuery request, CancellationToken cancellationToken) =>
        db.Skills.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
}
