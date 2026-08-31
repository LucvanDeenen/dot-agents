namespace AgentPlatform.Application.Agents;

/// <summary>
/// Runs agents and keeps their sessions alive for follow-up turns. The POC
/// implementation (infrastructure's DockerAgentRunner) starts one long-lived
/// Claude Code runner container per run and drives each turn via `docker exec`;
/// the abstraction keeps the application layer independent of Docker.
/// </summary>
public interface IAgentRunner
{
    /// <summary>Instantiate an agent for <paramref name="config"/>, run its first turn, and keep the session alive.</summary>
    Task<AgentReply> StartAsync(AgentRunConfig config, CancellationToken ct);

    /// <summary>Continue an existing run by sending <paramref name="message"/> to its live session.</summary>
    /// <exception cref="AgentRunNotFoundException">No live run exists for <paramref name="runId"/>.</exception>
    Task<AgentReply> ContinueAsync(string runId, string message, CancellationToken ct);
}

/// <summary>One turn's reply from an agent run. <see cref="RunId"/> addresses the live session for follow-ups.</summary>
public record AgentReply(string RunId, string Response);

/// <summary>Thrown when a follow-up targets a run that never existed or whose session has already closed.</summary>
public sealed class AgentRunNotFoundException(string runId)
    : Exception($"No live agent run found for id '{runId}'.")
{
    public string RunId { get; } = runId;
}
