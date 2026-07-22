using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;

namespace AgentPlatform.Application.Skills;

public sealed record CreateSkillCommand(string Name, string? Description, string Instructions) : IRequest<Skill>;

public sealed class CreateSkillCommandHandler(IAgentDbContext db) : IRequestHandler<CreateSkillCommand, Skill>
{
    public async Task<Skill> Handle(CreateSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = new Skill
        {
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions
        };

        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);
        return skill;
    }
}
