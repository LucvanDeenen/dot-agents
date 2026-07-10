using AgentPlatform.Api.Data;
using AgentPlatform.Api.Messaging;
using AgentPlatform.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Route("jobs")]
public class JobsController(AgentDbContext db, ITaskPublisher publisher, ILogger<JobsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateJobRequest request, CancellationToken cancellationToken)
    {
        var job = new AgentJob
        {
            Prompt = request.Prompt,
            RepoUrl = request.RepoUrl,
            Branch = request.Branch
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            // Routing key must match the binding pattern the topology
            // initializer declared (RabbitMqOptions.TaskRoutingKeyPattern).
            await publisher.PublishAsync(new TaskMessage(job.Id), routingKey: "job.created", cancellationToken);
        }
        catch (Exception ex)
        {
            // The job row exists either way — don't lose it because the broker
            // hiccuped. Leave it Pending; a reconciliation sweep or manual
            // republish can pick it up. Don't return 201 with a misleading
            // "queued" implication if this happens, though.
            logger.LogError(ex, "Failed to publish job {JobId} to the task queue", job.Id);
            return Problem(
                title: "Job saved but not queued",
                detail: "The job was persisted but could not be published to the task queue. It will need to be retried.",
                statusCode: StatusCodes.Status202Accepted);
        }

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var job = await db.Jobs.FindAsync([id], cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var jobs = await db.Jobs
            .OrderByDescending(j => j.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(jobs);
    }
}

public record CreateJobRequest(string Prompt, string? RepoUrl, string? Branch);