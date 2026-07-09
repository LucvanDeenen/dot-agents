namespace AgentPlatform.Api.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public required string Host { get; set; }
    public int Port { get; set; } = 5672;
    public required string User { get; set; }
    public required string Password { get; set; }
    public string VirtualHost { get; set; } = "/";

    // Topic exchange that agent task requests are published to.
    public string TaskExchange { get; set; } = "agent.tasks";

    // Placeholder queue + binding: one queue bound to every task routing key
    // (task.#). Split into per-agent-type queues/bindings as agent types
    // are introduced.
    public string TaskQueue { get; set; } = "agent.tasks.queue";
    public string TaskRoutingKeyPattern { get; set; } = "task.#";
}
