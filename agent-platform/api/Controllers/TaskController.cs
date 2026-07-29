using AgentPlatform.Api.Generated;
using AgentPlatform.Application.Features.Tasks.CreateTask;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

[ApiController]
public class ApiController(IMediator mediator) : ApiControllerBase
{
    public override async Task<ActionResult<AgentResponse>> Tasks(TaskRequest body, CancellationToken ct = default)
    {
        var command = new CreateTaskCommand(body.Context, body.Action, body.System);
        var result = await mediator.Send(command, ct);

        return new AgentResponse
        {
            Response = result.Response,
            Action = result.Action
        };
    }
}
