using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Analytics;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Analytics.GetTodayDashboard;

/// <summary><c>GET /staff/dashboard/today?branch=</c>.</summary>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
public sealed record GetTodayDashboardQuery(string? BranchId = null);

/// <summary>
/// "การ์ดสรุปหน้าแรกของแต่ละบทบาท": today's appointments, queue and cases for everyone who holds
/// <c>dashboard:read</c>, plus the two cards that depend on who is asking.
/// <para>
/// <b>Today's takings</b> are money, and <c>dashboard:read</c> is not a money scope. The card is
/// added only for a caller who could already see the same money another way: <c>analytics:read</c>
/// (the revenue report) or <c>invoice:write</c> (the cash desk's bill list). A receptionist's front
/// page does not quietly become a sales report.
/// </para>
/// <para>
/// <b>Mine</b> is the caller's own day: the appointments booked with them, the tickets waiting for
/// them and the cases they have open. It is what makes the same endpoint a vet's front page as well
/// as a manager's.
/// </para>
/// </summary>
public sealed class GetTodayDashboardHandler(
    IOperationalStatsQueries operations,
    IBusinessAnalyticsQueries business,
    ReportWindows windows,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<TodayDashboardDto> HandleAsync(
        GetTodayDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = windows.ForToday(query.BranchId);
        var now = clock.NowInClinicTime();

        var appointments = await operations.AppointmentCountsAsync(today, cancellationToken);
        var tickets = await operations.QueueCountsAsync(today, cancellationToken);
        var snapshot = await operations.TodaySnapshotAsync(today, currentUser.StaffId, cancellationToken);

        var mayReadMoney = currentUser.Scopes.Allows(Scopes.Analytics.Read)
            || currentUser.Scopes.Allows(Scopes.Invoice.Write);

        var revenue = mayReadMoney ? Takings(await business.RevenueAsync(today, cancellationToken)) : null;

        return new TodayDashboardDto(
            today.First,
            today.BranchId,
            now,
            Appointments(appointments),
            Queue(tickets, snapshot, now),
            new TodayConsultationsDto(snapshot.OpenConsultations, snapshot.ClosedConsultations),
            revenue,
            currentUser.StaffId is null
                ? null
                : new MyDayDto(snapshot.MyAppointments, snapshot.MyWaitingQueue, snapshot.MyOpenConsultations));
    }

    private static TodayAppointmentsDto Appointments(IReadOnlyList<AppointmentCountRow> rows)
    {
        int Count(params AppointmentStatus[] statuses) =>
            rows.Where(row => statuses.Contains(row.Status)).Sum(row => row.Count);

        return new TodayAppointmentsDto(
            rows.Sum(row => row.Count),
            Count(AppointmentStatus.Booked, AppointmentStatus.Confirmed),
            Count(AppointmentStatus.CheckedIn, AppointmentStatus.InConsultation),
            Count(AppointmentStatus.Completed),
            Count(AppointmentStatus.Cancelled),
            Count(AppointmentStatus.NoShow));
    }

    private static TodayQueueDto Queue(IReadOnlyList<QueueCountRow> rows, TodaySnapshotRow snapshot, DateTimeOffset now)
    {
        int Count(QueueTicketStatus status) => rows.Where(row => row.Status == status).Sum(row => row.Tickets);

        int? longest = snapshot.OldestWaitingSince is { } since
            ? Math.Max(0, (int)Math.Floor((now - since).TotalMinutes))
            : null;

        return new TodayQueueDto(
            rows.Sum(row => row.Tickets),
            Count(QueueTicketStatus.Waiting),
            Count(QueueTicketStatus.Called),
            Count(QueueTicketStatus.InRoom),
            Count(QueueTicketStatus.Served),
            Count(QueueTicketStatus.Skipped),
            Count(QueueTicketStatus.Left),
            ReportMath.AverageMinutes(rows.Sum(row => row.WaitSeconds), rows.Sum(row => row.Called)),
            longest);
    }

    private static TodayRevenueDto Takings(IReadOnlyList<RevenueRow> rows)
    {
        var collected = rows.Sum(row => row.CollectedSatang);
        var refunded = rows.Sum(row => row.RefundedSatang);

        return new TodayRevenueDto(collected, refunded, collected - refunded, rows.Sum(row => row.Payments));
    }
}
