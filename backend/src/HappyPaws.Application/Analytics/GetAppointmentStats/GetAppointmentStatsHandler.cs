using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Domain.Analytics;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Analytics.GetAppointmentStats;

/// <summary><c>GET /staff/stats/appointments?from=&amp;to=&amp;branch=&amp;granularity=</c>.</summary>
/// <param name="From">First clinic day; 29 days before <paramref name="To"/> when absent.</param>
/// <param name="To">Last clinic day, included; today when absent.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
/// <param name="Granularity"><c>day</c> (default), <c>week</c> or <c>month</c>.</param>
public sealed record GetAppointmentStatsQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? BranchId = null,
    string? Granularity = null);

/// <summary>
/// "ปริมาณนัด อัตรายกเลิก no-show รายช่วง".
/// <para>
/// An appointment counts on the clinic day it <i>starts</i>, not the day it was booked: the
/// question is how busy the day was and how many people failed to turn up for it. Drafts are left
/// out, because a draft is a booking the staff flow has not finished making.
/// </para>
/// <para>
/// The two rates have different denominators on purpose. The cancellation rate is over every
/// appointment. The no-show rate is over the appointments that were <i>not</i> cancelled, because a
/// cancelled appointment is one nobody was expected at; counting it would make a clinic with many
/// cancellations look better at attendance than it is.
/// </para>
/// </summary>
public sealed class GetAppointmentStatsHandler(IOperationalStatsQueries operations, ReportWindows windows)
{
    public const int DefaultDays = 30;

    public async Task<AppointmentStatsDto> HandleAsync(
        GetAppointmentStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var granularity = AnalyticsWire.ParseGranularity(query.Granularity, ReportGranularity.Day);
        var window = windows.Resolve(query.From, query.To, query.BranchId, DefaultDays);
        var rows = await operations.AppointmentCountsAsync(window, cancellationToken);

        int Count(IEnumerable<AppointmentCountRow> set, params AppointmentStatus[] statuses) =>
            set.Where(row => statuses.Contains(row.Status)).Sum(row => row.Count);

        var total = rows.Sum(row => row.Count);
        var cancelled = Count(rows, AppointmentStatus.Cancelled);
        var noShow = Count(rows, AppointmentStatus.NoShow);

        var totals = new AppointmentTotalsDto(
            total,
            Count(rows, AppointmentStatus.Booked, AppointmentStatus.Confirmed),
            Count(rows, AppointmentStatus.CheckedIn, AppointmentStatus.InConsultation),
            Count(rows, AppointmentStatus.Completed),
            cancelled,
            rows.Where(row => row is { Status: AppointmentStatus.Cancelled, IsLateCancellation: true }).Sum(row => row.Count),
            noShow,
            rows.Where(row => row.IsWalkIn).Sum(row => row.Count),
            rows.Where(row => row.BookedOnline).Sum(row => row.Count),
            ReportMath.Rate(cancelled, total),
            ReportMath.Rate(noShow, total - cancelled));

        var series = ReportPeriods.Between(window.First, window.Last, granularity)
            .Select(period =>
            {
                var inPeriod = rows.Where(row => ReportPeriods.StartOf(row.Day, granularity) == period).ToList();

                return new AppointmentPeriodDto(
                    period,
                    inPeriod.Sum(row => row.Count),
                    Count(inPeriod, AppointmentStatus.Completed),
                    Count(inPeriod, AppointmentStatus.Cancelled),
                    Count(inPeriod, AppointmentStatus.NoShow));
            })
            .ToList();

        return new AppointmentStatsDto(
            window.First, window.Last, window.BranchId, AnalyticsWire.ToWire(granularity), totals, series);
    }
}
