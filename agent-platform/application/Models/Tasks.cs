namespace AgentPlatform.Application.Models;

/// <summary>
/// Create a new agent task. The Api layer maps the generated DTO onto this
/// request; it never passes the DTO itself. Mirrors the `TaskRequest` schema
/// in api/Spec/agent-platform.yaml.
/// </summary>
public record TaskRequest(string? Context, string? Action, string? System);

/// <summary>A follow-up message sent to an already-running agent session.</summary>
public record MessageRequest(string Message);

/// <summary>One agent turn: the reply text plus the runId to continue the session.</summary>
public record TaskResult(string Response, string RunId, string? Action = null);
