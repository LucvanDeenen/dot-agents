using AgentPlatform.Domain;

namespace AgentPlatform.Application.Abstractions;

public sealed record AgentRunResult(bool Succeeded, string Output);

// Executes one agent run for a task (POC: a Docker container running a
// Claude Code session). Implementations own capacity limiting — RunAsync
// blocks until a slot is free.
public interface IAgentRunner
{
    Task<AgentRunResult> RunAsync(AgentTask task, Agent agent, CancellationToken cancellationToken = default);
}
