using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Application.Notifications;

/// <summary>Persistence port for the web inbox.</summary>
public interface IInboxRepository
{
    /// <summary>Adds a message and saves it at once: the inbox is written after the use case has committed.</summary>
    Task DeliverAsync(InboxNotification notification, CancellationToken cancellationToken = default);

    /// <summary>A customer's messages, newest first.</summary>
    Task<CursorPage<InboxNotification>> ListAsync(
        string customerId,
        bool unreadOnly,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    Task<int> UnreadCountAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>Marks some, or with no ids all, of a customer's messages read. Returns how many changed.</summary>
    Task<int> MarkReadAsync(
        string customerId,
        IReadOnlyCollection<string>? ids,
        DateTimeOffset at,
        CancellationToken cancellationToken = default);
}
