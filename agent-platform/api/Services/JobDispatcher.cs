using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Services;

/// <summary>
/// Polls for pending jobs and runs them one at a time.
/// Deliberately simple (no queue broker) — this is the "prove the workflow"
/// stage. Swap for a RabbitMQ-consumer version once you have multiple
/// agent types genuinely competing for work.
/// </summary>
public class JobDispatcher(IServiceScopeFactory scopeFactory, ILogger<JobDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing jobs");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
        var runner = scope.ServiceProvider.GetRequiredService<IAgentRunner>();

        var job = await db.Jobs
            .Where(j => j.Status == JobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(stoppingToken);

        if (job is null) return;

        job.Status = JobStatus.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(stoppingToken);

        var result = await runner.RunAsync(job, stoppingToken);

        job.Status = result.Success ? JobStatus.Succeeded : JobStatus.Failed;
        job.Result = result.Output;
        job.Error = result.Error;
        job.ContainerId = result.ContainerId;
        job.CompletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(stoppingToken);

        logger.LogInformation("Job {JobId} finished with status {Status}", job.Id, job.Status);
    }
}
