using AgentPlatform.Api.Data;
using AgentPlatform.Api.Generated;
using AgentPlatform.Api.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskEntity = AgentPlatform.Api.Models.AgentTask;
using TaskEntityStatus = AgentPlatform.Api.Models.AgentTaskStatus;
using TaskMessage = AgentPlatform.Api.Models.TaskMessage;

namespace AgentPlatform.Api.Controllers;

// Endpoints, routes and DTOs (AgentTask, CreateJobRequest, ...) live in
// Generated/JobsController.g.cs, produced from spec/openapi.yaml —
// edit the spec, not this file, to change the contract.
[ApiController]
public class JobsController(AgentDbContext db, ITaskPublisher publisher, ILogger<JobsController> logger)
    : JobsControllerBase
{
    public override async Task<ActionResult<AgentTask>> JobsPost(CreateJobRequest body, CancellationToken cancellationToken)
    {
        var task = new TaskEntity
        {
            Instruction = body.Prompt,
            RoutingKey = "task.created",
            Status = TaskEntityStatus.Pending
        };

        db.AgentTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            // Routing key must match the binding pattern the topology
            // initializer declared (RabbitMqOptions.TaskRoutingKeyPattern).
            await publisher.PublishAsync(new TaskMessage(task.Id), task.RoutingKey, cancellationToken);
            task.Status = TaskEntityStatus.Queued;
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

        return Created($"/jobs/{task.Id}", ToDto(task));
    }

    public override async Task<ActionResult<AgentTask>> JobsGet(Guid id, CancellationToken cancellationToken)
    {
        var task = await db.AgentTasks.FindAsync([id], cancellationToken);
        return task is null ? NotFound() : ToDto(task);
    }

    public override async Task<ActionResult<ICollection<AgentTask>>> JobsGet(CancellationToken cancellationToken)
    {
        var tasks = await db.AgentTasks
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return tasks.Select(ToDto).ToList();
    }

    private static AgentTask ToDto(TaskEntity task) => new()
    {
        Id = task.Id,
        RoutingKey = task.RoutingKey,
        Instruction = task.Instruction,
        Status = (AgentTaskStatus)(int)task.Status,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
