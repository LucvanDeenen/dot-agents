using System.ComponentModel.DataAnnotations;

namespace AgentPlatform.Infrastructure.Options;

/// <summary>Binds the "AgentRunner" configuration section (see appsettings.json).</summary>
public class RunnerOptions
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

    /// <summary>
    /// Docker Compose project label applied to the runner network and containers,
    /// so they group under the same stack in Docker Desktop / `docker compose ls`.
    /// Matches the `name:` in infra/docker-compose.yml.
    /// </summary>
    [Required]
    public string ComposeProject { get; set; } = "agent-platform";

    /// <summary>
    /// Docker network the runner containers join. Created by the backend on
    /// startup (see AgentNetworkInitializer) and owned by it — every agent run
    /// is attached here.
    /// </summary>
    [Required]
    public string Network { get; set; } = "agent-platform-agents";

    /// <summary>
    /// Optional: absolute path to the directory the infra stack's
    /// `docker compose up` ran from. When set (together with
    /// <see cref="ComposeConfigFile"/>), it's stamped as the
    /// <c>com.docker.compose.project.working_dir</c> label so Docker Desktop
    /// nests runs into the exact same agent-platform stack. Machine-specific,
    /// so it's left unset by default.
    /// </summary>
    public string? ComposeWorkingDir { get; set; }

    /// <summary>
    /// Optional: absolute path to the infra compose file, stamped as the
    /// <c>com.docker.compose.project.config_files</c> label. See
    /// <see cref="ComposeWorkingDir"/>.
    /// </summary>
    public string? ComposeConfigFile { get; set; }

    /// <summary>
    /// Optional: personal access token the runner uses to push over HTTPS.
    /// Passed to the container as GIT_TOKEN; git is configured to use it
    /// automatically. Falls back to the API's GIT_TOKEN env var when blank.
    /// </summary>
    public string? GitToken { get; set; }

    /// <summary>Git author name for commits the agent makes (GIT_USER_NAME).</summary>
    public string? GitUserName { get; set; }

    /// <summary>Git author email for commits the agent makes (GIT_USER_EMAIL).</summary>
    public string? GitUserEmail { get; set; }

    /// <summary>Host the git token authenticates against (GIT_HOST). Defaults to github.com in the runner.</summary>
    public string? GitHost { get; set; }
}