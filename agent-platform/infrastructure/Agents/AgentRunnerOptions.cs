using System.ComponentModel.DataAnnotations;

namespace AgentPlatform.Infrastructure.Agents;

/// <summary>Binds the "AgentRunner" configuration section (see appsettings.json).</summary>
public class AgentRunnerOptions
{
    public const string SectionName = "AgentRunner";

    /// <summary>Runner image to start per task. Built locally as <c>agent-runner:local</c>.</summary>
    [Required]
    public string Image { get; set; } = "agent-runner:local";

    /// <summary>Cap on concurrently running runner containers.</summary>
    [Range(1, 100)]
    public int MaxConcurrency { get; set; } = 2;

    /// <summary>Hard stop for a single run, enforced by the runner.</summary>
    [Range(1, 1440)]
    public int RunTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Claude subscription token (from `claude setup-token`) passed to the
    /// runner as CLAUDE_CODE_OAUTH_TOKEN. Falls back to the API process's own
    /// env var of the same name when left blank.
    /// </summary>
    public string? ClaudeCodeOAuthToken { get; set; }
}
