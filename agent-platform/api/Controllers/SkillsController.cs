using AgentPlatform.Api.Generated;
using AgentPlatform.Application.Skills;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

// Skill endpoints of the spec-generated ApiControllerBase (see JobController.cs
// for the partial-class layout rationale).
public partial class ApiController
{
    public override async Task<ActionResult<ICollection<Skill>>> SkillsGet(CancellationToken cancellationToken)
    {
        var skills = await mediator.Send(new ListSkillsQuery(), cancellationToken);
        return skills.Select(ToDto).ToList();
    }

    public override async Task<ActionResult<Skill>> SkillsPost(UpsertSkillRequest body, CancellationToken cancellationToken)
    {
        var skill = await mediator.Send(
            new CreateSkillCommand(body.Name, body.Description, body.Instructions),
            cancellationToken);
        return Created($"/skills/{skill.Id}", ToDto(skill));
    }

    public override async Task<ActionResult<Skill>> SkillsGet(Guid id, CancellationToken cancellationToken)
    {
        var skill = await mediator.Send(new GetSkillByIdQuery(id), cancellationToken);
        return skill is null ? NotFound() : ToDto(skill);
    }

    public override async Task<ActionResult<Skill>> SkillsPut(Guid id, UpsertSkillRequest body, CancellationToken cancellationToken)
    {
        var skill = await mediator.Send(
            new UpdateSkillCommand(id, body.Name, body.Description, body.Instructions),
            cancellationToken);
        return skill is null ? NotFound() : ToDto(skill);
    }

    public override async Task<IActionResult> SkillsDelete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteSkillCommand(id), cancellationToken);
        return result switch
        {
            DeleteSkillResult.Deleted => NoContent(),
            DeleteSkillResult.NotFound => NotFound(),
            _ => Problem(
                title: "Skill is in use",
                detail: "The skill is still referenced by one or more agents. Detach it from those agents first.",
                statusCode: StatusCodes.Status409Conflict)
        };
    }
}
