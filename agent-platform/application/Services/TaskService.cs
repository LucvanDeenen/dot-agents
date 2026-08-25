using AgentPlatform.Application.Models;

namespace AgentPlatform.Application.Services;

public interface ITaskService
{
    Task<TaskResult> CreateAsync(TaskRequest request, CancellationToken ct);
}

public class TaskService : ITaskService
{
    public Task<TaskResult> CreateAsync(TaskRequest request, CancellationToken ct)
    {
        var result = new TaskResult(
            Response: $"Task accepted for system '{request.System}'.",
            Action: request.Action);

        return Task.FromResult(result);
    }
}
