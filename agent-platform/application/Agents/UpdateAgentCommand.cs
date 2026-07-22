using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Agents;

public sealed record UpdateAgentCommand(
    Guid Id,
    string Name,
    string? Description,
    string SystemPrompt,
    List<string>? AllowedTools,
    string RoutingKeyPattern,
    bool Enabled,
    List<Guid>? SkillIds) : IRequest<Agent?>;

public sealed class UpdateAgentCommandHandler(IAgentDbContext db) : IRequestHandler<UpdateAgentCommand, Agent?>
{
    public async Task<Agent?> Handle(UpdateAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = await db.Agents
            .Include(a => a.Skills)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (agent is null) return null;

        agent.Name = request.Name;
        agent.Description = request.Description;
        agent.SystemPrompt = request.SystemPrompt;
        agent.AllowedTools = request.AllowedTools ?? [];
        agent.RoutingKeyPattern = request.RoutingKeyPattern;
        agent.Enabled = request.Enabled;
        agent.UpdatedAt = DateTimeOffset.UtcNow;

        var skillIds = request.SkillIds ?? [];
        agent.Skills = await db.Skills
            .Where(s => skillIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return agent;
    }
}
