using AgentPlatform.Domain;

namespace AgentPlatform.Application.Jobs;

// Thrown when a task is persisted but the broker publish fails. Carries the
// task so the Api layer's exception handler can report its id without a
// second lookup — the row exists either way, this only means it's still
// Pending instead of Queued.
public sealed class TaskPublishFailedException(AgentTask task, Exception inner)
    : Exception($"Task {task.Id} was persisted but could not be published to the task queue.", inner)
{
    public AgentTask Task { get; } = task;
}
