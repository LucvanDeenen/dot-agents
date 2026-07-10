using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.Messaging;

public interface ITaskPublisher
{
    Task PublishAsync(TaskMessage message, string routingKey, CancellationToken cancellationToken = default);
}
