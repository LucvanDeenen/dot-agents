using System.Text.Json;
using AgentPlatform.Application.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AgentPlatform.Infrastructure.Messaging;

public class RabbitMqTaskPublisher(
    IOptions<RabbitMqOptions> options,
    RabbitMqConnectionHolder connectionHolder) : ITaskPublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(TaskMessage message, string routingKey,
        CancellationToken cancellationToken = default)
    {
        if (connectionHolder.Connection is not { IsOpen: true } connection)
            throw new InvalidOperationException("RabbitMQ connection is not established yet.");

        // Channels aren't meant to be shared across concurrent publishes in this
        // client version — open one per publish. Cheap relative to a job dispatch.
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            _options.TaskExchange,
            routingKey,
            false,
            properties,
            body,
            cancellationToken);
    }
}