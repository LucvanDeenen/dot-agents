using AgentPlatform.Api.Data;
using AgentPlatform.Api.Messaging;
using AgentPlatform.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Route("jobs")]
public class JobsController(AgentDbContext db, ITaskPublisher publisher, ILogger<JobsController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateJobRequest request, CancellationToken cancellationToken)
    {
        var task = new AgentTask
        {
            Instruction = request.Prompt,
            RoutingKey = "task.created",
            Status = AgentTaskStatus.Pending
        };

        db.AgentTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            // Routing key must match the binding pattern the topology
            // initializer declared (RabbitMqOptions.TaskRoutingKeyPattern).
            await publisher.PublishAsync(new TaskMessage(task.Id), task.RoutingKey, cancellationToken);
            task.Status = AgentTaskStatus.Queued;
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // The task row exists either way — don't lose it because the broker
            // hiccuped. Leave it Pending; a reconciliation sweep or manual
            // republish can pick it up. Don't return 201 with a misleading
            // "queued" implication if this happens, though.
            logger.LogError(ex, "Failed to publish task {TaskId} to the task queue", task.Id);
            return Problem(
                title: "Task saved but not queued",
                detail:
                "The task was persisted but could not be published to the task queue. It will need to be retried.",
                statusCode: StatusCodes.Status202Accepted);
        }

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await db.AgentTasks.FindAsync([id], cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var tasks = await db.AgentTasks
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(tasks);
    }
}

public record CreateJobRequest(string Prompt, string? RepoUrl, string? Branch);