namespace AgentPlatform.Application.Abstractions;

public interface ITaskPublisher
{
    Task PublishAsync(TaskMessage message, string routingKey, CancellationToken cancellationToken = default);
}
