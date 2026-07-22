namespace AgentPlatform.Infrastructure.AgentRunning;

public class AgentRunnerOptions
{
    public const string SectionName = "AgentRunner";

    // Image built from agent-runner/Dockerfile at the repo root.
    public string Image { get; set; } = "agent-runner:local";

    // Hard cap on simultaneously running agent containers — the POC's
    // "auto-scaling" story: 0..MaxConcurrency containers exist at any time.
    public int MaxConcurrency { get; set; } = 2;

    // Long-lived Claude subscription token (`claude setup-token`). Falls back
    // to the CLAUDE_CODE_OAUTH_TOKEN environment variable when unset.
    public string? ClaudeCodeOAuthToken { get; set; }

    // A run that exceeds this is killed and the task marked Failed.
    public int RunTimeoutMinutes { get; set; } = 30;

    // Optional Docker Engine endpoint override (e.g. "npipe://./pipe/docker_engine"
    // or "unix:///var/run/docker.sock"). Platform default when unset.
    public string? DockerEndpoint { get; set; }
}
