using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Messaging.Dtos;

namespace HappyPaws.Application.Messaging.Portal;

/// <summary><c>GET /portal/thread?limit=&amp;cursor=</c>.</summary>
public sealed record GetMyThreadQuery(int Limit = CursorPaging.DefaultLimit, string? Cursor = null);

/// <summary>
/// The signed-in customer's own conversation with the clinic: <b>contract extension</b>
/// (<c>docs/contract-extensions.md § 24</c>).
/// <para>
/// <b>Whose thread.</b> The customer id comes from the token and from nowhere else, so there is no
/// parameter that could name someone else's thread. A carer is a customer of their own and sees
/// only the thread of their own account.
/// </para>
/// <para>
/// <b>Creates nothing.</b> A customer who never wrote gets an empty answer with no id; the thread
/// starts with their first message, as it does on LINE.
/// </para>
/// <para>
/// Messages come newest first, a page at a time; <c>next_cursor</c> leads to older ones. Reading
/// marks nothing: the clinic's replies are also in the web inbox, whose own "read" is what the
/// portal clears when the chat page is open.
/// </para>
/// </summary>
public sealed class GetMyThreadHandler(IMessageThreadRepository threads, ICurrentUser currentUser)
{
    public async Task<PortalThreadDto> HandleAsync(GetMyThreadQuery query, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        if (query.Cursor is { Length: > 0 } cursor)
        {
            // A malformed cursor is a 422 whether or not there is a thread to page through.
            Cursor.Decode(cursor);
        }

        var thread = await threads.FindForCustomerAsync(customerId, cancellationToken);

        if (thread is null)
        {
            return new PortalThreadDto(null, null, [], null);
        }

        var page = await threads.MessagesAsync(thread.Id, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        return new PortalThreadDto(
            thread.Id,
            thread.AwaitingReply ? ThreadDto.AwaitingReply : ThreadDto.Replied,
            [.. page.Data.Select(PortalThreadMessageDto.From)],
            page.NextCursor);
    }
}
