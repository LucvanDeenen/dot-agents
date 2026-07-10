using System.Text.Json;
using AgentPlatform.Api.Data;
using AgentPlatform.Api.Messaging;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AgentPlatform.Api.Listeners;

// Consumes AgentTask.Id messages off _options.TaskQueue and updates task state.
public class TaskQueueListener(
    IOptions<RabbitMqOptions> options,
    RabbitMqConnectionHolder connectionHolder,
    IServiceScopeFactory scopeFactory,
    ILogger<TaskQueueListener> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (connectionHolder.Connection is not { IsOpen: true } connection)
            throw new InvalidOperationException("RabbitMQ connection is not established yet.");

        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Only hand this consumer one unacked message at a time — an agent
        // job can run for minutes, no reason to prefetch a backlog.
        await _channel.BasicQosAsync(0, 1, false,
            stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            TaskMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<TaskMessage>(ea.Body.Span);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Unparseable task message, dropping. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false,
                    stoppingToken);
                return;
            }

            if (message is null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false,
                    stoppingToken);
                return;
            }

            try
            {
                await ProcessTaskAsync(message.TaskId, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed processing task {TaskId}, requeueing", message.TaskId);
                // requeue: false once you have a dead-letter exchange set up —
                // requeueing forever on a poison message will spin the same job.
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true,
                    stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            _options.TaskQueue,
            false,
            consumer,
            stoppingToken);

        logger.LogInformation("Listening on queue {Queue}", _options.TaskQueue);

        // The consumer's ReceivedAsync handler does the actual work; this just
        // keeps the background service alive until shutdown.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task ProcessTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();

        var task = await db.AgentTasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found, skipping", taskId);
            return;
        }

        if (task.Status is AgentTaskStatus.Completed or AgentTaskStatus.Failed)
            return;

        task.Status = AgentTaskStatus.Running;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        task.Status = AgentTaskStatus.Completed;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}