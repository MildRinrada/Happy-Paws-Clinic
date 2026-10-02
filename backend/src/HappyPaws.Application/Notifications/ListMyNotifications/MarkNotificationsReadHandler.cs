using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.Notifications.ListMyNotifications;

/// <summary>
/// <c>POST /portal/notifications/read</c> — a contract extension, the "mark as read" half of
/// <c>GET /portal/notifications</c>. See <c>docs/contract-extensions.md § 13</c>. No ids marks
/// everything read.
/// </summary>
public sealed record MarkNotificationsReadCommand(IReadOnlyList<string>? Ids = null);

/// <summary>How many messages this call marked, and how many are still unread.</summary>
public sealed record MarkedReadDto(int Marked, int UnreadCount);

/// <summary>
/// Marks the caller's own messages read. An id that is not theirs, or already read, changes nothing
/// and is not an error: the answer says how many were marked.
/// </summary>
public sealed class MarkNotificationsReadHandler(IInboxRepository inbox, ICurrentUser currentUser, IClock clock)
{
    public async Task<MarkedReadDto> HandleAsync(MarkNotificationsReadCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        var marked = await inbox.MarkReadAsync(customerId, command.Ids, clock.UtcNow, cancellationToken);

        return new MarkedReadDto(marked, await inbox.UnreadCountAsync(customerId, cancellationToken));
    }
}
