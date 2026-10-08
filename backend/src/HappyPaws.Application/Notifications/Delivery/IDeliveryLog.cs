using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Application.Notifications.Delivery;

/// <summary>
/// The record of every attempt to deliver a message — what <c>GET /staff/notifications/log</c>
/// reads.
/// <para>
/// <see cref="RecordAsync"/> writes through a context of its own and commits at once, like the web
/// inbox: messages go out after a use case has committed, and a log row must never be what saves
/// someone else's half-finished change, nor be lost because a later step of the request failed.
/// </para>
/// </summary>
public interface IDeliveryLog
{
    /// <summary>Writes one row. Throws only if the database is unreachable; callers swallow and log.</summary>
    Task RecordAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default);

    /// <summary>Newest first, with each customer's name for the screen.</summary>
    Task<CursorPage<DeliveryLogRow>> ListAsync(
        DeliveryLogFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>Every row of one staff send or broadcast, oldest first.</summary>
    Task<IReadOnlyList<NotificationDelivery>> ForBatchAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>How many messages of a kind went out since an instant — the broadcast quota's count.</summary>
    Task<int> CountSentSinceAsync(
        DeliverySource source,
        DeliveryChannel channel,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}

/// <summary>A delivery and the name of the customer it was for.</summary>
public sealed record DeliveryLogRow(NotificationDelivery Delivery, string? CustomerName);

/// <summary>
/// What the log may be narrowed by. <c>BranchIds</c> is the caller's visible branches, or null when
/// unrestricted; rows with no branch (a portal invitation, a broadcast) are visible to every reader.
/// </summary>
public sealed record DeliveryLogFilter(
    DeliveryStatus? Status = null,
    DeliveryChannel? Channel = null,
    DeliverySource? Source = null,
    string? CustomerId = null,
    string? TemplateKey = null,
    string? BatchId = null,
    IReadOnlyCollection<string>? BranchIds = null);
