using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.Services;

public interface IAgentRunner
{
    Task<AgentRunResult> RunAsync(AgentJob job, CancellationToken cancellationToken);
}

public record AgentRunResult(bool Success, string? Output, string? Error, string ContainerId);
