using AgentPlatform.Api.Generated;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

[ApiController]
public class ApiController(IMediator mediator) : ApiControllerBase
{
    public override Task<ActionResult<AgentResponse>> Tasks(TaskRequest body, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
