using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AgentPlatform.Api.Messaging;

// Establishes the shared connection and declares the placeholder task
// exchange/queue/binding on startup so the topology exists before anything
// tries to publish or consume.
public class RabbitMqTopologyInitializer(
    IOptions<RabbitMqOptions> options,
    RabbitMqConnectionHolder connectionHolder,
    ILogger<RabbitMqTopologyInitializer> logger) : IHostedService
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
        };

        var connection = await factory.CreateConnectionAsync(cancellationToken);
        connectionHolder.Connection = connection;

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.TaskExchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _options.TaskQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: _options.TaskQueue,
            exchange: _options.TaskExchange,
            routingKey: _options.TaskRoutingKeyPattern,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "RabbitMQ topology ready: exchange={Exchange} queue={Queue} routingKey={RoutingKey}",
            _options.TaskExchange, _options.TaskQueue, _options.TaskRoutingKeyPattern);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (connectionHolder.Connection is { } connection)
        {
            await connection.CloseAsync(cancellationToken);
            connection.Dispose();
        }
    }
}
