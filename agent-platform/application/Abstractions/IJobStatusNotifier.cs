using AgentPlatform.Domain;

namespace AgentPlatform.Application.Abstractions;

public interface IJobStatusNotifier
{
    Task NotifyStatusChangedAsync(AgentTask task, CancellationToken cancellationToken = default);
}