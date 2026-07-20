using AgentPlatform.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Skills;

public enum DeleteSkillResult
{
    Deleted,
    NotFound,
    ReferencedByAgents
}

public sealed record DeleteSkillCommand(Guid Id) : IRequest<DeleteSkillResult>;

public sealed class DeleteSkillCommandHandler(IAgentDbContext db) : IRequestHandler<DeleteSkillCommand, DeleteSkillResult>
{
    public async Task<DeleteSkillResult> Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await db.Skills
            .Include(s => s.Agents)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (skill is null) return DeleteSkillResult.NotFound;
        if (skill.Agents.Count > 0) return DeleteSkillResult.ReferencedByAgents;

        db.Skills.Remove(skill);
        await db.SaveChangesAsync(cancellationToken);
        return DeleteSkillResult.Deleted;
    }
}
