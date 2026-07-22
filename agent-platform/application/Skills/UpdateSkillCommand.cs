using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Skills;

public sealed record UpdateSkillCommand(Guid Id, string Name, string? Description, string Instructions) : IRequest<Skill?>;

public sealed class UpdateSkillCommandHandler(IAgentDbContext db) : IRequestHandler<UpdateSkillCommand, Skill?>
{
    public async Task<Skill?> Handle(UpdateSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (skill is null) return null;

        skill.Name = request.Name;
        skill.Description = request.Description;
        skill.Instructions = request.Instructions;
        skill.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return skill;
    }
}
