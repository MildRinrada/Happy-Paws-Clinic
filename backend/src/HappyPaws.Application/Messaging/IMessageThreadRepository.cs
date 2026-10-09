using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Messaging;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Application.Messaging;

/// <summary>The customers' threads and their messages: what <c>/staff/threads</c> reads and writes.</summary>
public interface IMessageThreadRepository
{
    void Add(MessageThread thread);

    void AddMessage(ThreadMessage message);

    /// <summary>The thread, tracked for an update, or null.</summary>
    Task<MessageThread?> FindTrackedAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>The customer's thread, tracked for an update, or null when they never wrote.</summary>
    Task<MessageThread?> FindTrackedForCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>True when a message with the channel's own id is already stored: a webhook delivered twice.</summary>
    Task<bool> HasInboundAsync(DeliveryChannel channel, string externalId, CancellationToken cancellationToken = default);

    /// <summary>The branch the customer last booked at, or null when they never booked.</summary>
    Task<string?> LatestBranchOfCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>Threads awaiting a reply first, then the most recently active, each with its summary.</summary>
    Task<CursorPage<ThreadRow>> ListAsync(ThreadQuery query, CursorPageRequest page, CancellationToken cancellationToken = default);

    /// <summary>The newest <paramref name="limit"/> messages of a thread, newest first, with the writer's name.</summary>
    Task<IReadOnlyList<ThreadMessageRow>> RecentMessagesAsync(string threadId, int limit, CancellationToken cancellationToken = default);

    /// <summary>A staff member's display name, for the answer to a reply.</summary>
    Task<string?> StaffNameAsync(string staffId, CancellationToken cancellationToken = default);
}

/// <summary>
/// What the inbox may be narrowed by. <c>BranchIds</c> is the caller's visible branches, or null
/// when unrestricted; a thread with no branch is visible to every reader.
/// </summary>
public sealed record ThreadQuery(
    IReadOnlyCollection<string>? BranchIds = null,
    bool? AwaitingReply = null,
    string? CustomerId = null,
    string? ThreadId = null);

/// <summary>A thread with the customer's name, the number of messages since the last reply, and its newest message.</summary>
public sealed record ThreadRow(
    MessageThread Thread,
    string? CustomerName,
    int UnreadCount,
    int MessageCount,
    ThreadMessage? LastMessage);

/// <summary>A message and, for a reply, the name of the staff member who wrote it.</summary>
public sealed record ThreadMessageRow(ThreadMessage Message, string? SentByName);
