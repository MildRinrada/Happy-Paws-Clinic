namespace HappyPaws.Application.Analytics.Dtos;

/// <summary>
/// <c>GET /staff/dashboard/today</c>. The cards that open each role's console.
/// </summary>
/// <param name="Date">The clinic's today.</param>
/// <param name="BranchId">The branch asked for; absent when the cards add up every branch the caller may see.</param>
/// <param name="GeneratedAt">When the numbers were read, in clinic time.</param>
/// <param name="Appointments">Today's appointments.</param>
/// <param name="Queue">Today's queue.</param>
/// <param name="Consultations">Cases open now and closed today.</param>
/// <param name="Revenue">Today's takings. Present only for a caller holding <c>analytics:read</c> or <c>invoice:write</c>.</param>
/// <param name="Mine">What is on the caller's own plate today.</param>
public sealed record TodayDashboardDto(
    DateOnly Date,
    string? BranchId,
    DateTimeOffset GeneratedAt,
    TodayAppointmentsDto Appointments,
    TodayQueueDto Queue,
    TodayConsultationsDto Consultations,
    TodayRevenueDto? Revenue,
    MyDayDto? Mine);

/// <param name="Total">Every appointment starting today, drafts excluded.</param>
/// <param name="Upcoming">Booked or confirmed, not here yet.</param>
/// <param name="Arrived">Checked in or in the room.</param>
/// <param name="Completed">Finished.</param>
/// <param name="Cancelled">Cancelled.</param>
/// <param name="NoShow">Marked as not having come.</param>
public sealed record TodayAppointmentsDto(int Total, int Upcoming, int Arrived, int Completed, int Cancelled, int NoShow);

/// <param name="Issued">Tickets issued today.</param>
/// <param name="Waiting">Waiting to be called.</param>
/// <param name="Called">Called, not yet in the room.</param>
/// <param name="InRoom">In the room.</param>
/// <param name="Served">Served.</param>
/// <param name="Skipped">Skipped and not back yet.</param>
/// <param name="Left">Went home without being seen.</param>
/// <param name="AverageWaitMinutes">Issue to call over the tickets called so far today.</param>
/// <param name="LongestWaitMinutes">How long the longest-waiting ticket still waiting has been waiting.</param>
public sealed record TodayQueueDto(
    int Issued,
    int Waiting,
    int Called,
    int InRoom,
    int Served,
    int Skipped,
    int Left,
    decimal? AverageWaitMinutes,
    int? LongestWaitMinutes);

/// <param name="Open">Open now, whatever day they were opened.</param>
/// <param name="ClosedToday">Closed today.</param>
public sealed record TodayConsultationsDto(int Open, int ClosedToday);

/// <param name="CollectedSatang">Payments taken today.</param>
/// <param name="RefundedSatang">Refunds paid today.</param>
/// <param name="NetSatang">Collected less refunded.</param>
/// <param name="Payments">How many payments.</param>
public sealed record TodayRevenueDto(long CollectedSatang, long RefundedSatang, long NetSatang, int Payments);

/// <param name="Appointments">The caller's appointments today, cancelled ones excluded.</param>
/// <param name="QueueWaiting">Tickets waiting or called for the caller.</param>
/// <param name="OpenConsultations">The caller's open cases.</param>
public sealed record MyDayDto(int Appointments, int QueueWaiting, int OpenConsultations);
