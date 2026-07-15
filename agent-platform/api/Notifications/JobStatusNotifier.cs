using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;

namespace AgentPlatform.Api.Notifications;

public sealed class JobStatusNotifier(JobStatusBroadcaster broadcaster) : IJobStatusNotifier
{
    public Task NotifyStatusChangedAsync(AgentTask task, CancellationToken cancellationToken = default)
    {
        var update = new JobStatusChangedEvent(task.Id, task.Status, task.UpdatedAt);
        return broadcaster.PublishAsync(update, cancellationToken).AsTask();
    }
}