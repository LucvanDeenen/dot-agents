using AgentPlatform.Api.Generated;
using AgentPlatform.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

/// <summary>
/// Implementation of auto-generated PromptController controller setup 
/// </summary>
[ApiController]
public class PromptController(IAgentService agentService) : ApiControllerBase
{
    public override Task<ActionResult<string>> Prompts(PromptRequest body,
        CancellationToken ct = default)
    {
        var agentResponse = agentService.Prompt(body.Message, ct);

        return Task.FromResult<ActionResult<string>>(
            agentResponse.Result
        );
    }
}