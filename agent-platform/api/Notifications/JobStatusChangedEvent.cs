using AgentPlatform.Domain;

namespace AgentPlatform.Api.Notifications;

public sealed record JobStatusChangedEvent(
    Guid TaskId,
    AgentTaskStatus Status,
    DateTimeOffset? UpdatedAt,
    Guid? AgentId,
    string? AgentName,
    string? Output);