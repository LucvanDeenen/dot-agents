namespace AgentPlatform.Domain;

// A configured agent definition: the context, tools and skills a Claude
// session is given when it picks up a task, plus the routing-key pattern
// deciding which tasks it is willing to consume.
public class Agent
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Injected as the guide session's context (--append-system-prompt).
    public required string SystemPrompt { get; set; }

    // Claude Code tool names the run is allowed to use (--allowedTools).
    // Empty means the runner's default toolset.
    public List<string> AllowedTools { get; set; } = [];

    // AMQP-style topic pattern (* = one word, # = zero or more words) matched
    // against AgentTask.RoutingKey when dispatching, e.g. "task.dev.#".
    public required string RoutingKeyPattern { get; set; }

    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Skill> Skills { get; set; } = [];
}
