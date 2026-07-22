namespace AgentPlatform.Domain;

// A reusable capability description shared between agents. Materialized into
// the runner container as a Claude Code skill (SKILL.md) for each agent that
// references it.
public class Skill
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // The skill body: instructions the agent loads when the skill applies.
    public required string Instructions { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Agent> Agents { get; set; } = [];
}
