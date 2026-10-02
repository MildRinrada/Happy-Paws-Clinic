namespace HappyPaws.Domain.Appointments;

/// <summary>
/// The appointment state machine, written once as data.
/// <para>
/// The contract draws it as
/// <c>draft → booked → confirmed → checked_in → in_consultation → completed</c> with
/// <c>cancelled</c> and <c>no_show</c> as exits, and requires that skipping a step answers
/// <c>409 state_conflict</c>. Keeping the edges in a table rather than scattered across
/// <see cref="Appointment"/>'s methods means the machine can be read at a glance and asserted
/// exhaustively in a unit test — including the transitions that must <i>not</i> exist.
/// </para>
/// </summary>
public static class AppointmentTransitions
{
    private static readonly Dictionary<AppointmentStatus, AppointmentStatus[]> Allowed = new()
    {
        [AppointmentStatus.Draft] = [AppointmentStatus.Booked, AppointmentStatus.Cancelled],

        [AppointmentStatus.Booked] =
        [
            AppointmentStatus.Confirmed,
            // A walk-in is booked and checked in at the counter in one breath, so booked →
            // checked_in is a real path, not a skipped step.
            AppointmentStatus.CheckedIn,
            AppointmentStatus.Cancelled,
            AppointmentStatus.NoShow,
        ],

        [AppointmentStatus.Confirmed] =
        [
            AppointmentStatus.CheckedIn,
            AppointmentStatus.Cancelled,
            AppointmentStatus.NoShow,
        ],

        // Once the pet is in the building, "did not show" is no longer available.
        [AppointmentStatus.CheckedIn] = [AppointmentStatus.InConsultation, AppointmentStatus.Cancelled],

        [AppointmentStatus.InConsultation] = [AppointmentStatus.Completed],

        // Terminal. A completed visit is corrected through the medical record's amendment path,
        // never by reopening the appointment.
        [AppointmentStatus.Completed] = [],
        [AppointmentStatus.Cancelled] = [],
        [AppointmentStatus.NoShow] = [],
    };

    /// <summary>
    /// The statuses in which an appointment still blocks its time on the calendar.
    /// <para>
    /// Exposed as a list, not just as a predicate, because the collision query needs to push this
    /// set into SQL. A repository that hard-coded its own copy would be one refactor away from
    /// disagreeing with <see cref="OccupiesSlot"/> — and a disagreement there is a double booking.
    /// </para>
    /// </summary>
    public static readonly IReadOnlyList<AppointmentStatus> OccupyingStatuses =
    [
        AppointmentStatus.Booked,
        AppointmentStatus.Confirmed,
        AppointmentStatus.CheckedIn,
        AppointmentStatus.InConsultation,
    ];

    private static readonly HashSet<AppointmentStatus> Occupying = [.. OccupyingStatuses];

    public static bool IsAllowed(AppointmentStatus from, AppointmentStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>
    /// True while the appointment holds its slot. Cancelled, no-show and completed visits release
    /// the time, which is what "ปล่อย slot คืน" means in practice.
    /// </summary>
    public static bool OccupiesSlot(AppointmentStatus status) => Occupying.Contains(status);

    public static IReadOnlyCollection<AppointmentStatus> NextFrom(AppointmentStatus status) =>
        Allowed.TryGetValue(status, out var next) ? next : [];
}
