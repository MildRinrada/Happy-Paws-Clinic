using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Application.Notifications.ListMyNotifications;

/// <summary>One message in the inbox.</summary>
public sealed record InboxItemDto(
    string Id,
    string Template,
    string Title,
    string Body,
    string? ReferenceId,
    DateTimeOffset SentAt,
    DateTimeOffset? ReadAt)
{
    public static InboxItemDto From(InboxNotification item) =>
        new(item.Id, item.TemplateKey, item.Title, item.Body, item.ReferenceId, item.CreatedAt, item.ReadAt);
}

/// <summary>A page of the inbox and how many are unread in all.</summary>
public sealed record InboxPageDto(IReadOnlyList<InboxItemDto> Data, int UnreadCount, string? NextCursor);

/// <summary><c>GET /portal/notifications</c> — "กล่องแจ้งเตือนในเว็บ". <c>UnreadOnly</c> narrows to what is new.</summary>
public sealed record ListMyNotificationsQuery(bool UnreadOnly = false, int Limit = CursorPaging.DefaultLimit, string? Cursor = null);

/// <summary>
/// The caller's own messages, newest first. Reading the list does not mark anything read; that is
/// <c>POST /portal/notifications/read</c>, because a list that changed itself on every look could
/// not be refreshed without losing the badge.
/// </summary>
public sealed class ListMyNotificationsHandler(IInboxRepository inbox, ICurrentUser currentUser)
{
    public async Task<InboxPageDto> HandleAsync(ListMyNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        var page = await inbox.ListAsync(customerId, query.UnreadOnly, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);
        var unread = await inbox.UnreadCountAsync(customerId, cancellationToken);

        return new InboxPageDto([.. page.Data.Select(InboxItemDto.From)], unread, page.NextCursor);
    }
}
