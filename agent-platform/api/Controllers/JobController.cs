using AgentPlatform.Api.Generated;
using AgentPlatform.Application.Jobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskEntity = AgentPlatform.Domain.AgentTask;

namespace AgentPlatform.Api.Controllers;

// Endpoints, routes and DTOs (AgentTask, CreateJobRequest, ...) live in
// Generated/JobsController.g.cs, produced from spec/agent-platform.yaml —
// edit the spec, not this file, to change the contract. All business logic
// lives behind IMediator in AgentPlatform.Application — this controller only
// dispatches commands/queries and shapes the HTTP response.
[ApiController]
public class JobsController(IMediator mediator) : JobsControllerBase
{
    public override async Task<ActionResult<AgentTask>> JobsPost(CreateJobRequest body, CancellationToken cancellationToken)
    {
        // If the broker publish fails, CreateJobCommandHandler throws
        // TaskPublishFailedException, which TaskPublishFailedExceptionHandler
        // turns into the 202 response — no branching needed here.
        var task = await mediator.Send(new CreateJobCommand(body.Prompt, body.RepoUrl, body.Branch), cancellationToken);
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

    private static AgentTask ToDto(TaskEntity task) => new()
    {
        Id = task.Id,
        RoutingKey = task.RoutingKey,
        Instruction = task.Instruction,
        Status = (AgentTaskStatus)(int)task.Status,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
