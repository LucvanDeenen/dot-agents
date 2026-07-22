using AgentPlatform.Api.Generated;
using AgentPlatform.Application.Agents;
using Microsoft.AspNetCore.Mvc;
using AgentEntity = AgentPlatform.Domain.Agent;
using SkillEntity = AgentPlatform.Domain.Skill;

namespace AgentPlatform.Api.Controllers;

// Agent endpoints of the spec-generated ApiControllerBase (see JobController.cs
// for the partial-class layout rationale).
public partial class ApiController
{
    public override async Task<ActionResult<ICollection<Agent>>> AgentsGet(CancellationToken cancellationToken)
    {
        var agents = await mediator.Send(new ListAgentsQuery(), cancellationToken);
        return agents.Select(ToDto).ToList();
    }

    public override async Task<ActionResult<Agent>> AgentsPost(UpsertAgentRequest body, CancellationToken cancellationToken)
    {
        var agent = await mediator.Send(
            new CreateAgentCommand(
                body.Name,
                body.Description,
                body.SystemPrompt,
                body.AllowedTools?.ToList(),
                body.RoutingKeyPattern,
                body.Enabled,
                body.SkillIds?.ToList()),
            cancellationToken);
        return Created($"/agents/{agent.Id}", ToDto(agent));
    }

    public override async Task<ActionResult<Agent>> AgentsGet(Guid id, CancellationToken cancellationToken)
    {
        var agent = await mediator.Send(new GetAgentByIdQuery(id), cancellationToken);
        return agent is null ? NotFound() : ToDto(agent);
    }

    public override async Task<ActionResult<Agent>> AgentsPut(Guid id, UpsertAgentRequest body, CancellationToken cancellationToken)
    {
        var agent = await mediator.Send(
            new UpdateAgentCommand(
                id,
                body.Name,
                body.Description,
                body.SystemPrompt,
                body.AllowedTools?.ToList(),
                body.RoutingKeyPattern,
                body.Enabled,
                body.SkillIds?.ToList()),
            cancellationToken);
        return agent is null ? NotFound() : ToDto(agent);
    }

    public override async Task<IActionResult> AgentsDelete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteAgentCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static Agent ToDto(AgentEntity agent) => new()
    {
        Id = agent.Id,
        Name = agent.Name,
        Description = agent.Description,
        SystemPrompt = agent.SystemPrompt,
        AllowedTools = agent.AllowedTools.ToList(),
        RoutingKeyPattern = agent.RoutingKeyPattern,
        Enabled = agent.Enabled,
        CreatedAt = agent.CreatedAt,
        UpdatedAt = agent.UpdatedAt,
        Skills = agent.Skills.Select(ToDto).ToList()
    };

    private static Skill ToDto(SkillEntity skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name,
        Description = skill.Description,
        Instructions = skill.Instructions,
        CreatedAt = skill.CreatedAt,
        UpdatedAt = skill.UpdatedAt
    };
}
