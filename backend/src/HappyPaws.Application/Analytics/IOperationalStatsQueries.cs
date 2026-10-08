using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Analytics;

/// <summary>
/// The read model behind "Operational statistics": counts and durations over appointments, queue
/// tickets and consultations.
/// <para>
/// Each method answers with small grouped rows, never with aggregates. A month of a busy clinic is
/// thousands of tickets; loading them to count them in memory would turn a dashboard refresh into
/// the heaviest request the API serves. The grouping is done by PostgreSQL in hand-written SQL
/// (<c>docs/api-mapping.md</c> expects exactly this here) and the handlers only fold the rows.
/// </para>
/// </summary>
public interface IOperationalStatsQueries
{
    /// <summary>Appointments starting inside the window, drafts excluded, grouped by clinic day and outcome.</summary>
    Task<IReadOnlyList<AppointmentCountRow>> AppointmentCountsAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default);

    /// <summary>Queue tickets of the window's service days, grouped by day, hour issued and status.</summary>
    Task<IReadOnlyList<QueueCountRow>> QueueCountsAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Per vet: cases closed and booked inside the window, plus what is waiting on them right now.
    /// A row with no staff id carries today's tickets that nobody has been assigned yet.
    /// </summary>
    Task<IReadOnlyList<WorkloadRow>> WorkloadAsync(
        ReportWindow window,
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>The "right now" figures of today's dashboard that the grouped reads cannot give.</summary>
    Task<TodaySnapshotRow> TodaySnapshotAsync(
        ReportWindow today,
        string? staffId,
        CancellationToken cancellationToken = default);
}

/// <summary>Appointments sharing one day and one outcome.</summary>
public sealed record AppointmentCountRow(
    DateOnly Day,
    AppointmentStatus Status,
    bool IsWalkIn,
    bool IsLateCancellation,
    bool BookedOnline,
    int Count);

/// <summary>Tickets sharing one service day, one issue hour (clinic time) and one status.</summary>
/// <param name="Day">Service day.</param>
/// <param name="Hour">Hour of the day the tickets were issued, 0 to 23, in clinic time.</param>
/// <param name="Status">Status now.</param>
/// <param name="Tickets">How many.</param>
/// <param name="Called">How many have been called at least once.</param>
/// <param name="WaitSeconds">Issue to call, summed over the called ones.</param>
/// <param name="Roomed">How many went into the room and came out served.</param>
/// <param name="RoomSeconds">Room entry to served, summed over those.</param>
public sealed record QueueCountRow(
    DateOnly Day,
    int Hour,
    QueueTicketStatus Status,
    int Tickets,
    int Called,
    double WaitSeconds,
    int Roomed,
    double RoomSeconds);

/// <summary>One vet's load. <see cref="StaffId"/> null is the unassigned queue.</summary>
/// <param name="StaffId">Staff membership id, or null for tickets with no vet.</param>
/// <param name="DisplayName">The account's name, when there is one.</param>
/// <param name="Cases">Consultations closed inside the window.</param>
/// <param name="CaseSeconds">Opened to closed, summed over those.</param>
/// <param name="OpenCases">Consultations open now, opened on any day.</param>
/// <param name="WaitingQueue">Today's tickets waiting or called for this vet.</param>
/// <param name="Appointments">Appointments inside the window, cancelled ones excluded.</param>
public sealed record WorkloadRow(
    string? StaffId,
    string? DisplayName,
    int Cases,
    double CaseSeconds,
    int OpenCases,
    int WaitingQueue,
    int Appointments);

/// <summary>Today's live counts.</summary>
/// <param name="OpenConsultations">Consultations open now at the window's branches.</param>
/// <param name="ClosedConsultations">Consultations closed today.</param>
/// <param name="MyAppointments">The caller's appointments today, cancelled ones excluded.</param>
/// <param name="MyWaitingQueue">Today's tickets waiting or called for the caller.</param>
/// <param name="MyOpenConsultations">The caller's open consultations.</param>
/// <param name="OldestWaitingSince">When the longest-waiting ticket still waiting was issued.</param>
public sealed record TodaySnapshotRow(
    int OpenConsultations,
    int ClosedConsultations,
    int MyAppointments,
    int MyWaitingQueue,
    int MyOpenConsultations,
    DateTimeOffset? OldestWaitingSince);
