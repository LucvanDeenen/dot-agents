namespace AgentPlatform.Application.Agents;

/// <summary>
/// Runs agents and keeps their sessions alive for follow-up turns. The POC
/// implementation (infrastructure's DockerAgentRunner) starts one long-lived
/// Claude Code runner container per run and drives each turn via `docker exec`;
/// the abstraction keeps the application layer independent of Docker.
/// </summary>
public interface IAgentRunner
{
    Task<AgentReply> StartAsync(AgentConfig config, CancellationToken ct);

    Task<AgentReply> ContinueAsync(string runId, string message, CancellationToken ct);
}

public record AgentReply(string RunId, string Response);