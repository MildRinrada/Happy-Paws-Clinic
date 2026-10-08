using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Domain.Analytics;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Analytics.GetQueueStats;

/// <summary><c>GET /staff/stats/queue?from=&amp;to=&amp;branch=</c>.</summary>
/// <param name="From">First service day; 29 days before <paramref name="To"/> when absent.</param>
/// <param name="To">Last service day, included; today when absent.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
public sealed record GetQueueStatsQuery(DateOnly? From = null, DateOnly? To = null, string? BranchId = null);

/// <summary>
/// "เวลารอเฉลี่ย ชั่วโมงพีค คิวคงค้าง".
/// <para>
/// <b>Wait</b> is issue to call, over the tickets that were called. A ticket that left before it
/// was called has no wait to measure; counting it as zero would make walking out look like fast
/// service. <b>Peak hour</b> is the clinic-time hour in which the most tickets were <i>issued</i>,
/// which is when the counter was busiest. <b>Outstanding</b> is tickets that never reached an end
/// state: on today's date that is the live queue, on a past date it is a ticket somebody forgot to
/// close.
/// </para>
/// <para>
/// Days are the ticket's <c>service_date</c>, the clinic day the queue numbered it on, so this agrees
/// with the queue board for the same date.
/// </para>
/// </summary>
public sealed class GetQueueStatsHandler(IOperationalStatsQueries operations, ReportWindows windows)
{
    public const int DefaultDays = 30;

    private static readonly QueueTicketStatus[] Unfinished =
        [QueueTicketStatus.Waiting, QueueTicketStatus.Called, QueueTicketStatus.InRoom];

    public async Task<QueueStatsDto> HandleAsync(GetQueueStatsQuery query, CancellationToken cancellationToken = default)
    {
        var window = windows.Resolve(query.From, query.To, query.BranchId, DefaultDays);
        var rows = await operations.QueueCountsAsync(window, cancellationToken);

        int Count(QueueTicketStatus status) => rows.Where(row => row.Status == status).Sum(row => row.Tickets);

        var hours = Enumerable.Range(0, 24)
            .Select(hour =>
            {
                var inHour = rows.Where(row => row.Hour == hour).ToList();

                return new QueueHourDto(
                    hour,
                    inHour.Sum(row => row.Tickets),
                    ReportMath.AverageMinutes(inHour.Sum(row => row.WaitSeconds), inHour.Sum(row => row.Called)));
            })
            .ToList();

        var busiest = hours.Where(hour => hour.Tickets > 0)
            .OrderByDescending(hour => hour.Tickets)
            .ThenBy(hour => hour.Hour)
            .FirstOrDefault();

        var totals = new QueueTotalsDto(
            rows.Sum(row => row.Tickets),
            Count(QueueTicketStatus.Served),
            Count(QueueTicketStatus.Skipped),
            Count(QueueTicketStatus.Left),
            rows.Where(row => Unfinished.Contains(row.Status)).Sum(row => row.Tickets),
            ReportMath.AverageMinutes(rows.Sum(row => row.WaitSeconds), rows.Sum(row => row.Called)),
            ReportMath.AverageMinutes(rows.Sum(row => row.RoomSeconds), rows.Sum(row => row.Roomed)),
            busiest?.Hour);

        var series = ReportPeriods.Between(window.First, window.Last, ReportGranularity.Day)
            .Select(day =>
            {
                var onDay = rows.Where(row => row.Day == day).ToList();

                return new QueueDayDto(
                    day,
                    onDay.Sum(row => row.Tickets),
                    onDay.Where(row => row.Status == QueueTicketStatus.Served).Sum(row => row.Tickets),
                    ReportMath.AverageMinutes(onDay.Sum(row => row.WaitSeconds), onDay.Sum(row => row.Called)));
            })
            .ToList();

        return new QueueStatsDto(window.First, window.Last, window.BranchId, totals, hours, series);
    }
}
