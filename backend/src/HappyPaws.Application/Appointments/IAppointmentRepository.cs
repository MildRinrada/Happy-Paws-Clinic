using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments;

/// <summary>
/// Persistence port for the appointment aggregate. Implemented in
/// <c>Infrastructure/Persistence/Repositories/Appointments/</c>.
/// <para>
/// The filters that matter for authorization — branch and customer — are parameters here rather
/// than something a caller applies to the returned list. Filtering after the fact means the
/// database still shipped rows the caller was not allowed to see, and the first time someone
/// forgets the <c>Where</c>, they leak.
/// </para>
/// </summary>
public interface IAppointmentRepository
{
    Task<Appointment?> FindByIdAsync(string appointmentId, CancellationToken cancellationToken = default);

    void Add(Appointment appointment);

    /// <summary>
    /// Appointments that still hold time on a vet's calendar inside a window — the input to every
    /// collision check and to availability. Excludes cancelled, no-show and completed visits.
    /// <para>
    /// <c>excludeAppointmentId</c> drops the appointment being moved, so rescheduling does not
    /// collide with the slot it is in the middle of vacating.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Appointment>> OccupyingSlotsAsync(
        string branchId,
        IReadOnlyCollection<string> staffIds,
        DateTimeOffset from,
        DateTimeOffset to,
        string? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every future visit still booked for one patient, tracked for modification.
    /// <para>
    /// Used when a pet is marked deceased: the visits already in the calendar have to be dealt with
    /// rather than left to be called out at the counter. See <c>docs/contract-extensions.md § 6</c>.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Appointment>> UpcomingForPetAsync(
        string petId,
        DateTimeOffset from,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A patient's appointments cancelled with <paramref name="reason"/> at or after
    /// <paramref name="since"/> (ever, when null), read-only, earliest start first.
    /// <para>
    /// Used when a recorded death is undone: these are the visits the death cancelled, listed so
    /// the counter can rebook them by hand. See <c>docs/contract-extensions.md § 29</c>.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Appointment>> CancelledForPetAsync(
        string petId,
        string reason,
        DateTimeOffset? since,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET /staff/appointments?date=&amp;status=</c> — today's board for the clinic or one vet.</summary>
    Task<CursorPage<Appointment>> ListForStaffAsync(
        AppointmentListFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET /portal/appointments</c> — "นัดที่จะถึงและประวัติย้อนหลัง".</summary>
    Task<CursorPage<Appointment>> ListForCustomerAsync(
        string customerId,
        bool upcomingOnly,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// "นับสถิติรายลูกค้า" — a customer's no-shows and late cancellations, all time and since a date.
    /// Counted from the appointments themselves, so the numbers cannot drift from the calendar.
    /// </summary>
    Task<AttendanceCounts> AttendanceAsync(
        string customerId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>A patient's appointments still holding time in a window, at any branch.</summary>
    Task<IReadOnlyList<Appointment>> OccupyingForPetAsync(
        string petId,
        DateTimeOffset from,
        DateTimeOffset to,
        string? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appointments still holding time in a window, narrowed by branch and vet. The visible-branch
    /// restriction is applied at the database, as in <see cref="ListForStaffAsync"/>.
    /// </summary>
    Task<IReadOnlyList<Appointment>> OccupyingInWindowAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? branchId,
        string? staffId,
        IReadOnlyCollection<string>? visibleBranchIds,
        CancellationToken cancellationToken = default);
}

/// <summary>Missed and late-cancelled visits for one customer.</summary>
public sealed record AttendanceCounts(int NoShows, int LateCancellations, int NoShowsSince, int LateCancellationsSince)
{
    public int StrikesSince => NoShowsSince + LateCancellationsSince;
}

/// <summary>
/// The staff board's filters, straight from the contract's query string
/// (<c>?status=checked_in&amp;date=2026-08-22&amp;vet_id=stf_204&amp;q=มิโล</c>).
/// <para>
/// <c>VisibleBranchIds</c> is the branch restriction derived from the caller's token, or null when
/// unrestricted. It is applied at the database, never by the caller.
/// </para>
/// </summary>
public sealed record AppointmentListFilter(
    DateOnly? Date = null,
    AppointmentStatus? Status = null,
    string? StaffId = null,
    string? BranchId = null,
    string? Query = null,
    IReadOnlyCollection<string>? VisibleBranchIds = null);
