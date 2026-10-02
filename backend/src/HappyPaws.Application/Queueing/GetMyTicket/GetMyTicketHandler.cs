using HappyPaws.Application.Queueing.Dtos;

namespace HappyPaws.Application.Queueing.GetMyTicket;

/// <summary><c>GET /portal/queue/{id}</c> — "เลขคิวตัวเอง · กำลังเรียก · เหลืออีกกี่คิว".</summary>
public sealed record GetMyTicketQuery(string TicketId);

/// <summary>
/// What an owner sees while waiting: their number, whether they are being called, and how many
/// people are still ahead.
/// <para>
/// The count is of tickets still <i>waiting to be called</i>. Someone already in a room is not
/// ahead of you in any sense the person refreshing their phone cares about — they are being seen,
/// and counting them would make the number stall at one for the length of a consultation.
/// </para>
/// </summary>
public sealed class GetMyTicketHandler(IQueueTicketRepository tickets, QueueAccess access)
{
    public async Task<MyQueueTicketDto> HandleAsync(
        GetMyTicketQuery query,
        CancellationToken cancellationToken = default)
    {
        var ticket = await access.ForOwnerAsync(query.TicketId, cancellationToken);

        return MyQueueTicketDto.From(ticket, await tickets.CountAheadOfAsync(ticket, cancellationToken));
    }
}
