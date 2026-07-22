using AgentPlatform.Api.Generated;
using AgentPlatform.Application.Jobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskEntity = AgentPlatform.Domain.AgentTask;

namespace AgentPlatform.Api.Controllers;

// Endpoints, routes and DTOs (AgentTask, CreateJobRequest, ...) live in
// Generated/Controllers.g.cs, produced from spec/agent-platform.yaml —
// edit the spec, not this file, to change the contract. All business logic
// lives behind IMediator in AgentPlatform.Application — this controller only
// dispatches commands/queries and shapes the HTTP response. The class is
// partial: agent and skill endpoints live in AgentsController.cs and
// SkillsController.cs.
[ApiController]
public partial class ApiController(IMediator mediator) : ApiControllerBase
{
    public override async Task<ActionResult<AgentTask>> JobsPost(CreateJobRequest body, CancellationToken cancellationToken)
    {
        // If the broker publish fails, CreateJobCommandHandler throws
        // TaskPublishFailedException, which TaskPublishFailedExceptionHandler
        // turns into the 202 response — no branching needed here.
        var task = await mediator.Send(
            new CreateJobCommand(body.Prompt, body.RepoUrl, body.Branch, body.AgentId, body.RoutingKey),
            cancellationToken);
        return Created($"/jobs/{task.Id}", ToDto(task));
    }

    public override async Task<ActionResult<AgentTask>> JobsGet(Guid id, CancellationToken cancellationToken)
    {
        var task = await mediator.Send(new GetJobByIdQuery(id), cancellationToken);
        return task is null ? NotFound() : ToDto(task);
    }

    public override async Task<ActionResult<ICollection<AgentTask>>> JobsGet(CancellationToken cancellationToken)
    {
        var tasks = await mediator.Send(new ListJobsQuery(), cancellationToken);
        return tasks.Select(ToDto).ToList();
    }

    public override async Task<IActionResult> JobsDelete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteJobCommand(id), cancellationToken);
        return result switch
        {
            DeleteJobResult.Deleted => NoContent(),
            DeleteJobResult.NotFound => NotFound(),
            _ => Problem(
                title: "Task is running",
                detail: "The task is currently being worked on by an agent. Wait for it to finish before deleting.",
                statusCode: StatusCodes.Status409Conflict)
        };
    }

    private static AgentTask ToDto(TaskEntity task) => new()
    {
        Id = task.Id,
        RoutingKey = task.RoutingKey,
        Instruction = task.Instruction,
        Status = (AgentTaskStatus)(int)task.Status,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        AgentId = task.AgentId,
        AgentName = task.Agent?.Name,
        RepoUrl = task.RepoUrl,
        Branch = task.Branch,
        Output = task.Output
    };
}
