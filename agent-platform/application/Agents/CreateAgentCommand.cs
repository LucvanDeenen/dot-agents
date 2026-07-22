using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Agents;

public sealed record CreateAgentCommand(
    string Name,
    string? Description,
    string SystemPrompt,
    List<string>? AllowedTools,
    string RoutingKeyPattern,
    bool Enabled,
    List<Guid>? SkillIds) : IRequest<Agent>;

public sealed class CreateAgentCommandHandler(IAgentDbContext db) : IRequestHandler<CreateAgentCommand, Agent>
{
    public async Task<Agent> Handle(CreateAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = new Agent
        {
            Name = request.Name,
            Description = request.Description,
            SystemPrompt = request.SystemPrompt,
            AllowedTools = request.AllowedTools ?? [],
            RoutingKeyPattern = request.RoutingKeyPattern,
            Enabled = request.Enabled
        };

        if (request.SkillIds is { Count: > 0 } skillIds)
            agent.Skills = await db.Skills
                .Where(s => skillIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

        db.Agents.Add(agent);
        await db.SaveChangesAsync(cancellationToken);
        return agent;
    }
}
