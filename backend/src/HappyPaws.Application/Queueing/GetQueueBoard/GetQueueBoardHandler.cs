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
/// <para>
/// <c>Status</c> is a contract extension (<c>docs/contract-extensions.md § 31</c>): a comma-separated
/// list of wire statuses (<c>served,left</c>). When given, the board is exactly those statuses,
/// finished or not, and <c>IncludeFinished</c> has nothing left to decide. <c>waiting_count</c>
/// counts within what is returned, as it already does for <c>room</c>.
/// </para>
/// </summary>
public sealed record GetQueueBoardQuery(
    string? BranchId = null,
    string? RoomId = null,
    DateOnly? Date = null,
    bool IncludeFinished = false,
    string? Status = null);

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
        var statuses = ParseStatuses(query.Status);
        var branchId = await ResolveBranchAsync(query.BranchId, cancellationToken);

        branchGuard.EnsureCanAccess(branchId);

        var serviceDate = query.Date ?? ClinicToday();

        var board = await tickets.BoardAsync(
            branchId, serviceDate, query.RoomId, query.IncludeFinished || statuses is not null, cancellationToken);

        if (statuses is not null)
        {
            board = [.. board.Where(ticket => statuses.Contains(ticket.Status))];
        }

        return new QueueBoardDto(
            branchId,
            serviceDate,
            query.RoomId,
            board.Count(ticket => QueueTicketTransitions.AwaitingCallStatuses.Contains(ticket.Status)),
            [.. board.Select(QueueTicketDto.From)]);
    }

    /// <summary>
    /// <c>served,left</c> → the two statuses; null when no filter was asked for. An unknown value is
    /// <c>422</c> at <c>status</c>, not silently ignored: a history page that asked for a typo
    /// would otherwise show a whole day as if it were the answer.
    /// </summary>
    private static HashSet<QueueTicketStatus>? ParseStatuses(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var parsed = new HashSet<QueueTicketStatus>();

        foreach (var part in status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = Enum.GetValues<QueueTicketStatus>()
                .Where(candidate => QueueTicketDto.ToWireStatus(candidate) == part)
                .Select(candidate => (QueueTicketStatus?)candidate)
                .FirstOrDefault();

            if (match is not { } known)
            {
                throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["status"] = ["สถานะต้องเป็น waiting, called, in_room, skipped, served หรือ left คั่นด้วยจุลภาค"],
                });
            }

            parsed.Add(known);
        }

        return parsed.Count == 0 ? null : parsed;
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
