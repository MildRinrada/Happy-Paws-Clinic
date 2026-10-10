using HappyPaws.Application.Appointments;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.Displays;

/// <summary>
/// <c>GET /queue/display-board?branch=</c> — today's live board for the screen's own branch, cut
/// down to what a waiting room may see (<see cref="DisplayBoardDto"/>). Display tokens only. Not
/// in the API reference; see <c>docs/contract-extensions.md § 30</c>.
/// <para>
/// The branch comes from the token. <c>branch</c> may be omitted; naming another branch is
/// <c>403</c>, so a screen moved to the wrong site says so instead of showing the right site's queue.
/// </para>
/// <para>
/// Vet names are the staff accounts' display names, the same names the public team page shows.
/// They are looked up only for the tickets that are called or in a room, and only for active staff.
/// </para>
/// </summary>
public sealed class GetDisplayBoardHandler(
    IQueueTicketRepository tickets,
    IBookingCatalog catalog,
    IStaffRepository staff,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<DisplayBoardDto> HandleAsync(string? branchId, CancellationToken cancellationToken = default)
    {
        if (currentUser.DisplayId is null || currentUser.BranchIds.Count != 1)
        {
            throw new ForbiddenException("only a display token reads the display board");
        }

        var own = currentUser.BranchIds.First();

        if (branchId is { Length: > 0 } && !string.Equals(branchId, own, StringComparison.Ordinal))
        {
            throw new ForbiddenException($"display is not bound to branch {branchId}");
        }

        var branch = await catalog.FindBranchAsync(own, cancellationToken)
            ?? throw new NotFoundException("branch", own);

        var serviceDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, clock.ClinicTimeZone).DateTime);
        var board = await tickets.BoardAsync(own, serviceDate, cancellationToken: cancellationToken);

        var shown = board.Where(ticket => ticket.Status is QueueTicketStatus.Called or QueueTicketStatus.InRoom).ToList();
        var vetIds = shown.Select(ticket => ticket.StaffId).OfType<string>().Distinct(StringComparer.Ordinal).ToList();

        var names = vetIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : (await staff.FindRowsAsync(vetIds, cancellationToken))
                .Where(row => row.Membership.IsActive)
                .ToDictionary(row => row.Membership.Id, row => row.Account.DisplayName, StringComparer.Ordinal);

        return new DisplayBoardDto(
            branch.Id,
            branch.Name,
            serviceDate,
            board.Count(ticket => QueueTicketTransitions.AwaitingCallStatuses.Contains(ticket.Status)),
            branch.ExamRooms,
            [.. board.Select(ticket => ToScreen(ticket, names))]);
    }

    private static DisplayBoardTicketDto ToScreen(QueueTicket ticket, IReadOnlyDictionary<string, string> names) =>
        ticket.Status switch
        {
            QueueTicketStatus.Called or QueueTicketStatus.InRoom => new DisplayBoardTicketDto(
                ticket.Number,
                ticket.Status == QueueTicketStatus.Called ? "called" : "in_room",
                ticket.RoomId,
                ticket.StaffId is { } vet && names.TryGetValue(vet, out var name) ? name : null,
                ticket.CalledAt),

            // Waiting and skipped alike: a number still to be called.
            _ => new DisplayBoardTicketDto(ticket.Number, "waiting"),
        };
}
