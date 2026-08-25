namespace AgentPlatform.Application.Models;

/// <summary>
/// Create a new agent task and publish it to the task queue.
/// Mirrors the `TaskRequest` schema in api/Spec/agent-platform.yaml — the Api
/// layer maps the generated DTO onto this request, it never passes the DTO itself.
/// </summary>
public record TaskRequest(string? Context, string? Action, string? System);

public record TaskResult(string Response, string? Action);
