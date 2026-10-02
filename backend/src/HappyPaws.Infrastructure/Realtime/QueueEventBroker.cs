using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HappyPaws.Application.Queueing;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Realtime;

/// <summary>
/// In-process fan-out for <c>SSE /queue/stream?branch=</c>.
/// <para>
/// One clinic is one deployment, so every subscriber lives in this process and there is nothing to
/// coordinate across machines. A broker or a Redis channel here would add an operational
/// dependency — something else to run, monitor and fail over — to solve a problem this
/// architecture does not have. The port exists so that decision can be revisited without touching
/// a use case; see <see cref="IQueueEventStream"/> for what changes if the clinic ever runs two
/// replicas.
/// </para>
/// <para>
/// Each subscriber gets a bounded channel that <b>drops its oldest event</b> when full rather than
/// blocking the publisher. A waiting-room screen on a bad connection must not be able to stall the
/// receptionist pressing "call" — and a client that fell that far behind should re-fetch the board
/// anyway, which is what the drop warning tells the operator.
/// </para>
/// </summary>
public sealed class QueueEventBroker(ILogger<QueueEventBroker> logger) : IQueueEventStream
{
    /// <summary>Roughly a minute of a very busy branch. Beyond this a client should re-fetch.</summary>
    private const int SubscriberBuffer = 64;

    private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers = new();

    public void Publish(QueueEvent queueEvent)
    {
        foreach (var (_, subscriber) in _subscribers)
        {
            if (!string.Equals(subscriber.BranchId, queueEvent.BranchId, StringComparison.Ordinal))
            {
                continue;
            }

            // TryWrite on a DropOldest channel never fails and never blocks, so a stalled reader
            // cannot slow down the clinical action that produced this event.
            if (!subscriber.Channel.Writer.TryWrite(queueEvent))
            {
                logger.LogWarning(
                    "Dropped a {EventType} event for a queue subscriber on branch {BranchId}",
                    queueEvent.Type,
                    queueEvent.BranchId);
            }
        }
    }

    public async IAsyncEnumerable<QueueEvent> SubscribeAsync(
        string branchId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var subscriber = new Subscriber(
            branchId,
            Channel.CreateBounded<QueueEvent>(new BoundedChannelOptions(SubscriberBuffer)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
            }));

        _subscribers[id] = subscriber;

        try
        {
            await foreach (var queueEvent in subscriber.Channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return queueEvent;
            }
        }
        finally
        {
            // Runs on disconnect, cancellation and on an exception in the consumer alike. Without
            // it every closed browser tab would leak a channel for the life of the process.
            _subscribers.TryRemove(id, out _);
        }
    }

    /// <summary>Live subscriber count, for diagnostics and tests.</summary>
    public int SubscriberCount => _subscribers.Count;

    private sealed record Subscriber(string BranchId, Channel<QueueEvent> Channel);
}
