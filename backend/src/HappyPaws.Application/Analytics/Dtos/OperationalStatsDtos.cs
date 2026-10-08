namespace HappyPaws.Application.Analytics.Dtos;

/// <summary><c>GET /staff/stats/appointments</c>.</summary>
/// <param name="From">First clinic day counted.</param>
/// <param name="To">Last clinic day counted.</param>
/// <param name="BranchId">The branch asked for, if one was.</param>
/// <param name="Granularity">How <paramref name="Series"/> is grouped.</param>
/// <param name="Totals">The whole span.</param>
/// <param name="Series">One row per period, empty periods included.</param>
public sealed record AppointmentStatsDto(
    DateOnly From,
    DateOnly To,
    string? BranchId,
    string Granularity,
    AppointmentTotalsDto Totals,
    IReadOnlyList<AppointmentPeriodDto> Series);

/// <param name="Total">Appointments starting in the span, drafts excluded.</param>
/// <param name="Upcoming">Booked or confirmed and not yet resolved.</param>
/// <param name="Arrived">Checked in or in the room, not yet completed.</param>
/// <param name="Completed">Completed.</param>
/// <param name="Cancelled">Cancelled, by anyone.</param>
/// <param name="LateCancelled">Cancelled inside the clinic's cut-off.</param>
/// <param name="NoShow">No-shows.</param>
/// <param name="WalkIn">Walk-ins.</param>
/// <param name="BookedOnline">Booked by the customer, not at the counter.</param>
/// <param name="CancellationRate"><paramref name="Cancelled"/> over <paramref name="Total"/>.</param>
/// <param name="NoShowRate"><paramref name="NoShow"/> over the appointments that were not cancelled.</param>
public sealed record AppointmentTotalsDto(
    int Total,
    int Upcoming,
    int Arrived,
    int Completed,
    int Cancelled,
    int LateCancelled,
    int NoShow,
    int WalkIn,
    int BookedOnline,
    decimal? CancellationRate,
    decimal? NoShowRate);

/// <param name="Period">First day of the period.</param>
/// <param name="Total">Appointments.</param>
/// <param name="Completed">Completed.</param>
/// <param name="Cancelled">Cancelled.</param>
/// <param name="NoShow">No-shows.</param>
public sealed record AppointmentPeriodDto(DateOnly Period, int Total, int Completed, int Cancelled, int NoShow);

/// <summary><c>GET /staff/stats/queue</c>.</summary>
/// <param name="From">First service day counted.</param>
/// <param name="To">Last service day counted.</param>
/// <param name="BranchId">The branch asked for, if one was.</param>
/// <param name="Totals">The whole span.</param>
/// <param name="PeakHours">Every hour of the day in clinic time, 0 to 23, with the tickets issued in it.</param>
/// <param name="Series">One row per service day, empty days included.</param>
public sealed record QueueStatsDto(
    DateOnly From,
    DateOnly To,
    string? BranchId,
    QueueTotalsDto Totals,
    IReadOnlyList<QueueHourDto> PeakHours,
    IReadOnlyList<QueueDayDto> Series);

/// <param name="Tickets">Tickets issued.</param>
/// <param name="Served">Served.</param>
/// <param name="Skipped">Still skipped.</param>
/// <param name="Left">Left without being seen.</param>
/// <param name="Outstanding">Never finished: still waiting, called or in the room.</param>
/// <param name="AverageWaitMinutes">Issue to call, over tickets that were called.</param>
/// <param name="AverageRoomMinutes">Room entry to served.</param>
/// <param name="PeakHour">The hour with the most tickets issued; the earliest when tied.</param>
public sealed record QueueTotalsDto(
    int Tickets,
    int Served,
    int Skipped,
    int Left,
    int Outstanding,
    decimal? AverageWaitMinutes,
    decimal? AverageRoomMinutes,
    int? PeakHour);

/// <param name="Hour">Hour of the day, clinic time.</param>
/// <param name="Tickets">Tickets issued in that hour over the span.</param>
/// <param name="AverageWaitMinutes">Their mean wait.</param>
public sealed record QueueHourDto(int Hour, int Tickets, decimal? AverageWaitMinutes);

/// <param name="Date">Service day.</param>
/// <param name="Tickets">Tickets issued.</param>
/// <param name="Served">Served.</param>
/// <param name="AverageWaitMinutes">Mean wait.</param>
public sealed record QueueDayDto(DateOnly Date, int Tickets, int Served, decimal? AverageWaitMinutes);

/// <summary><c>GET /staff/stats/workload</c>.</summary>
/// <param name="From">First clinic day counted.</param>
/// <param name="To">Last clinic day counted.</param>
/// <param name="BranchId">The branch asked for, if one was.</param>
/// <param name="Totals">Every vet together.</param>
/// <param name="Staff">One row per vet with anything to show, busiest first.</param>
public sealed record WorkloadStatsDto(
    DateOnly From,
    DateOnly To,
    string? BranchId,
    WorkloadTotalsDto Totals,
    IReadOnlyList<WorkloadStaffDto> Staff);

/// <param name="Cases">Consultations closed in the span.</param>
/// <param name="AverageCaseMinutes">Opened to closed.</param>
/// <param name="OpenCases">Open now.</param>
/// <param name="UnassignedQueue">Today's tickets waiting with no vet assigned.</param>
public sealed record WorkloadTotalsDto(int Cases, decimal? AverageCaseMinutes, int OpenCases, int UnassignedQueue);

/// <param name="StaffId">Staff membership id.</param>
/// <param name="DisplayName">Name, when the account has one.</param>
/// <param name="Cases">Consultations closed in the span.</param>
/// <param name="AverageCaseMinutes">Opened to closed.</param>
/// <param name="Appointments">Appointments in the span, cancelled ones excluded.</param>
/// <param name="OpenCases">Open now.</param>
/// <param name="WaitingQueue">Today's tickets waiting or called for them.</param>
public sealed record WorkloadStaffDto(
    string StaffId,
    string? DisplayName,
    int Cases,
    decimal? AverageCaseMinutes,
    int Appointments,
    int OpenCases,
    int WaitingQueue);
