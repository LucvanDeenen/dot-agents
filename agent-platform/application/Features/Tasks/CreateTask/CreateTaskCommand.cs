using MediatR;

namespace AgentPlatform.Application.Features.Tasks.CreateTask;

/// <summary>
/// Create a new agent task and publish it to the task queue.
/// Mirrors the `TaskRequest` schema in api/Spec/agent-platform.yaml — the Api
/// layer maps the generated DTO onto this command, it never sends the DTO itself.
/// </summary>
public record CreateTaskCommand(string? Context, string? Action, string? System) : IRequest<CreateTaskResult>;

public record CreateTaskResult(string Response, string? Action);
