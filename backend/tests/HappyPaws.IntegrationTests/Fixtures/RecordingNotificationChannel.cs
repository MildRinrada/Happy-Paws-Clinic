using System.Collections.Concurrent;
using HappyPaws.Application.Notifications;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Stands in for LINE and SMS so a test can see what actually reached a channel.
/// <para>
/// It replaces the <b>channel</b>, not the dispatcher, and that distinction is the whole reason it
/// exists. Substituting the dispatcher would bypass the consent gate and let every suppression test
/// pass against a system that suppresses nothing.
/// </para>
/// </summary>
public sealed class RecordingNotificationChannel : INotificationChannel
{
    private readonly ConcurrentQueue<NotificationRequest> _sent = new();

    public Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(request);
        return Task.CompletedTask;
    }

    /// <summary>Everything delivered to one customer. The suite shares a host, so never the whole queue.</summary>
    public IReadOnlyList<NotificationRequest> SentTo(string customerId) =>
        [.. _sent.Where(request => request.CustomerId == customerId)];
}
