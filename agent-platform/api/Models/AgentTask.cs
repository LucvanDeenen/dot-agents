namespace AgentPlatform.Api.Models;

public enum AgentTaskStatus
{
    Pending,
    Queued,
    Running,
    Completed,
    Failed
}

// Placeholder table for queued work. One row per user request; RoutingKey
// determines which topic-bound queue/agent picks it up.
public class AgentTask
{
    public Guid Id { get; set; }
    public required string RoutingKey { get; set; }
    public required string Instruction { get; set; }
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}