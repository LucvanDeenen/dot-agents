using System.Text.Json;
using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AgentPlatform.Api.Messaging;

// Consumes AgentJob.Id messages off _options.TaskQueue and runs them through
// the same IAgentRunner the polling JobDispatcher used — this replaces that
// polling loop once you're ready to cut over, they don't need to run together.
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
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

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
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            if (message is null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            try
            {
                await ProcessJobAsync(message.JobId, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed processing job {JobId}, requeueing", message.JobId);
                // requeue: false once you have a dead-letter exchange set up —
                // requeueing forever on a poison message will spin the same job.
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _options.TaskQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Listening on queue {Queue}", _options.TaskQueue);

        // The consumer's ReceivedAsync handler does the actual work; this just
        // keeps the background service alive until shutdown.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
        var runner = scope.ServiceProvider.GetRequiredService<IAgentRunner>();

        var job = await db.Jobs.FindAsync([jobId], cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Job {JobId} not found, skipping", jobId);
            return;
        }

        job.Status = JobStatus.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var result = await runner.RunAsync(job, cancellationToken);

        job.Status = result.Success ? JobStatus.Succeeded : JobStatus.Failed;
        job.Result = result.Output;
        job.Error = result.Error;
        job.ContainerId = result.ContainerId;
        job.CompletedAt = DateTimeOffset.UtcNow;
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
