using AgentPlatform.Application.Agents;
using AgentPlatform.Application.Models;

namespace AgentPlatform.Application.Services;

public interface ITaskService
{
    Task<TaskResult> CreateAsync(TaskRequest request, CancellationToken ct);
    Task<TaskResult> ContinueAsync(string runId, MessageRequest request, CancellationToken ct);
}

public class TaskService(IAgentRunner agentRunner) : ITaskService
{
    public async Task<TaskResult> CreateAsync(TaskRequest request, CancellationToken ct)
    {
        // No agent store / routing yet: derive a single-run agent straight from
        // the incoming task. Persistence + pattern matching are the next step.
        var config = new AgentRunConfig(
            AgentName: string.IsNullOrWhiteSpace(request.System) ? "default" : request.System,
            Instruction: BuildInstruction(request));

        var reply = await agentRunner.StartAsync(config, ct);

        return new TaskResult(reply.Response, reply.RunId, request.Action);
    }

    public async Task<TaskResult> ContinueAsync(string runId, MessageRequest request, CancellationToken ct)
    {
        var reply = await agentRunner.ContinueAsync(runId, request.Message, ct);
        return new TaskResult(reply.Response, reply.RunId);
    }

    private static string BuildInstruction(TaskRequest request)
    {
        var action = string.IsNullOrWhiteSpace(request.Action) ? "(no action specified)" : request.Action;
        return string.IsNullOrWhiteSpace(request.Context)
            ? action
            : $"{action}\n\nContext:\n{request.Context}";
    }
}
