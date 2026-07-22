using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AgentPlatform.Api.Notifications;

public sealed class JobStatusBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<JobStatusChangedEvent>> _subscribers = new();

    public Subscription Subscribe(CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<JobStatusChangedEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var id = Guid.NewGuid();
        _subscribers[id] = channel;

        cancellationToken.Register(() => Unsubscribe(id));
        return new Subscription(channel.Reader, () => Unsubscribe(id));
    }

    public ValueTask PublishAsync(JobStatusChangedEvent update, CancellationToken cancellationToken = default)
    {
        foreach (var subscriber in _subscribers)
            subscriber.Value.Writer.TryWrite(update);

        return ValueTask.CompletedTask;
    }

    private void Unsubscribe(Guid id)
    {
        if (!_subscribers.TryRemove(id, out var channel))
            return;

        channel.Writer.TryComplete();
    }

    public sealed class Subscription(ChannelReader<JobStatusChangedEvent> reader, Action dispose) : IDisposable
    {
        public ChannelReader<JobStatusChangedEvent> Reader { get; } = reader;

        public void Dispose() => dispose();
    }
}