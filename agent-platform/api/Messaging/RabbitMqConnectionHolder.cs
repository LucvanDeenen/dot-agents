using RabbitMQ.Client;

namespace AgentPlatform.Api.Messaging;

// Shared connection established by RabbitMqTopologyInitializer at startup.
// Inject this in future publishers/consumers instead of opening new connections.
public class RabbitMqConnectionHolder
{
    public IConnection? Connection { get; internal set; }
}