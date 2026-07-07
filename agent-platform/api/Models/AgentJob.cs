namespace AgentPlatform.Api.Models;

public enum JobStatus
{
    Pending,
    Running,
    Succeeded,
    Failed
}

public class AgentJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // What to ask the agent to do.
    public string Prompt { get; set; } = string.Empty;

    // Optional — repo context for coding-agent jobs.
    public string? RepoUrl { get; set; }
    public string? Branch { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? Result { get; set; }
    public string? Error { get; set; }
    public string? ContainerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
