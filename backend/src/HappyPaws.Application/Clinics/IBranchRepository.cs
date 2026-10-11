using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics;

/// <summary>Persistence port for the clinic's sites.</summary>
public interface IBranchRepository
{
    void Add(Branch branch);

    /// <summary>Open branches in a stable order — what <c>GET /clinic/branches</c> shows the public.</summary>
    Task<IReadOnlyList<Branch>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>An open branch, tracked for modification (§ 51). Null when there is none or it is closed.</summary>
    Task<Branch?> FindOpenTrackedAsync(string branchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tickets at the branch, still on a queue dated <paramref name="from"/> or later, that sit in
    /// one of <paramref name="roomIds"/> — what stops a room being taken away (§ 51).
    /// </summary>
    Task<IReadOnlyList<ExamRoomUse>> ExamRoomUsesAsync(
        string branchId,
        IReadOnlyCollection<string> roomIds,
        DateOnly from,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The ids in <paramref name="branchIds"/> that are not an open branch of this clinic. Empty
    /// when all of them are — which is how staff, invitations and services check what they were
    /// told to point at before storing it.
    /// </summary>
    Task<IReadOnlyList<string>> MissingAsync(
        IReadOnlyCollection<string> branchIds,
        CancellationToken cancellationToken = default);
}

/// <summary>A ticket still on the queue in a room an edit would remove (§ 51).</summary>
/// <param name="TicketId">The ticket — <c>tkt_...</c>.</param>
/// <param name="Number">As printed — <c>A014</c>.</param>
/// <param name="RoomId">The room it is assigned to.</param>
/// <param name="ServiceDate">The queue's day.</param>
public sealed record ExamRoomUse(string TicketId, string Number, string RoomId, DateOnly ServiceDate);

/// <summary>Persistence port for the days the clinic is shut.</summary>
public interface IHolidayRepository
{
    void Add(ClinicHoliday holiday);

    /// <summary>True when that exact closure — same day, same branch or the same "every branch" — is already declared.</summary>
    Task<bool> ExistsAsync(DateOnly date, string? branchId, CancellationToken cancellationToken = default);

    /// <summary>Holidays on or after a day, soonest first.</summary>
    Task<IReadOnlyList<ClinicHoliday>> FromAsync(
        DateOnly from,
        int take,
        CancellationToken cancellationToken = default);
}
