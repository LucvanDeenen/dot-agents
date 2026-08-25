using AgentPlatform.Api.Generated;
using AgentPlatform.Application.Services;
using Microsoft.AspNetCore.Mvc;
using TaskRequest = AgentPlatform.Application.Models.TaskRequest;

namespace AgentPlatform.Api.Controllers;

[ApiController]
public class ApiController(ITaskService taskService) : ApiControllerBase
{
    public override async Task<ActionResult<AgentResponse>> Tasks(Generated.TaskRequest body, CancellationToken ct = default)
    {
        var request = new TaskRequest(body.Context, body.Action, body.System);
        var result = await taskService.CreateAsync(request, ct);

        return new AgentResponse
        {
            Response = result.Response,
            Action = result.Action
        };
    }
}
