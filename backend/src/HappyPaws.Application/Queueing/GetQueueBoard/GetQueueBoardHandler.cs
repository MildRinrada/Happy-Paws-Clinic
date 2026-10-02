using HappyPaws.Application.Appointments;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.GetQueueBoard;

/// <summary>
/// <c>GET /staff/queue?branch=&amp;room=</c> — "คิวทั้งหมดวันนี้พร้อมสถานะแต่ละใบ".
/// <para>
/// <c>IncludeFinished</c> false (the default) is the counter's live board; true adds served and
/// departed tickets, for looking back over the day.
/// </para>
/// </summary>
public sealed record GetQueueBoardQuery(
    string? BranchId = null,
    string? RoomId = null,
    DateOnly? Date = null,
    bool IncludeFinished = false);

/// <summary>
/// Today's queue for one branch, in the order tickets will be called.
/// <para>
/// The date defaults to today <i>in clinic time</i>, not to the server's date. The queue resets
/// daily per branch, so a board computed from a UTC date would show tomorrow's empty queue for the
/// last seven hours of every Bangkok evening.
/// </para>
/// </summary>
public sealed class GetQueueBoardHandler(
    IQueueTicketRepository tickets,
    IBookingCatalog catalog,
    BranchGuard branchGuard,
    IClock clock)
{
    public async Task<QueueBoardDto> HandleAsync(
        GetQueueBoardQuery query,
        CancellationToken cancellationToken = default)
    {
        var branchId = await ResolveBranchAsync(query.BranchId, cancellationToken);

        branchGuard.EnsureCanAccess(branchId);

        var serviceDate = query.Date ?? ClinicToday();

        var board = await tickets.BoardAsync(
            branchId, serviceDate, query.RoomId, query.IncludeFinished, cancellationToken);

        return new QueueBoardDto(
            branchId,
            serviceDate,
            query.RoomId,
            board.Count(ticket => QueueTicketTransitions.AwaitingCallStatuses.Contains(ticket.Status)),
            [.. board.Select(QueueTicketDto.From)]);
    }

    private DateOnly ClinicToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, clock.ClinicTimeZone).DateTime);

    private async Task<string> ResolveBranchAsync(string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is { Length: > 0 })
        {
            return branchId;
        }

        var sole = await catalog.SoleActiveBranchAsync(cancellationToken);

        return sole?.Id ?? throw new ValidationFailedException(
            new Dictionary<string, string[]> { ["branch"] = ["คลินิกมีหลายสาขา ต้องระบุสาขา"] });
    }
}
