namespace AgentPlatform.Domain;

public enum AgentTaskStatus
{
    Pending,
    Queued,
    Running,
    Completed,
    Failed
}

// One row per user request; RoutingKey determines which agent picks it up
// (matched against Agent.RoutingKeyPattern by the dispatcher).
public class AgentTask
{
    public Guid Id { get; set; }
    public required string RoutingKey { get; set; }
    public required string Instruction { get; set; }
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Pinned at creation time when the user picks an agent explicitly;
    // otherwise the dispatcher resolves one by routing-key match.
    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }

    public string? RepoUrl { get; set; }
    public string? Branch { get; set; }

    // The guide session's final report (or the failure reason).
    public string? Output { get; set; }
}
