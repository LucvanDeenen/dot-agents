using AgentPlatform.Api.Generated;
using AgentPlatform.Application.Agents;
using AgentPlatform.Application.Services;
using Microsoft.AspNetCore.Mvc;
using MessageRequest = AgentPlatform.Application.Models.MessageRequest;
using TaskRequest = AgentPlatform.Application.Models.TaskRequest;

namespace AgentPlatform.Api.Controllers;

[ApiController]
public class ApiController(ITaskService taskService) : ApiControllerBase
{
    public override async Task<ActionResult<AgentResponse>> Tasks(Generated.TaskRequest body, CancellationToken ct = default)
    {
        var request = new TaskRequest(body.Context, body.Action, body.System);
        var result = await taskService.CreateAsync(request, ct);

        return Ok(ToResponse(result));
    }

    public override async Task<ActionResult<AgentResponse>> TasksMessages(string runId, Generated.MessageRequest body, CancellationToken ct = default)
    {
        try
        {
            var result = await taskService.ContinueAsync(runId, new MessageRequest(body.Message), ct);
            return Ok(ToResponse(result));
        }
        catch (AgentRunNotFoundException ex)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: ex.Message);
        }
    }

    private static AgentResponse ToResponse(Application.Models.TaskResult result) => new()
    {
        Response = result.Response,
        RunId = result.RunId,
        Action = result.Action
    };
}
