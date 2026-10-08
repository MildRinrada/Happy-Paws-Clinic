namespace HappyPaws.Domain.Analytics;

/// <summary>
/// Calendar arithmetic for report series. Every input is already a clinic day: turning instants
/// into Bangkok dates happens once, in the query, so nothing here knows about time zones.
/// </summary>
public static class ReportPeriods
{
    /// <summary>The first day of the period <paramref name="day"/> falls in.</summary>
    public static DateOnly StartOf(DateOnly day, ReportGranularity granularity) => granularity switch
    {
        ReportGranularity.Day => day,

        // DayOfWeek counts from Sunday; a week here starts on Monday, so Sunday is six days in.
        ReportGranularity.Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
        ReportGranularity.Month => new DateOnly(day.Year, day.Month, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, null),
    };

    /// <summary>
    /// Every period between two days, both ends included, in order.
    /// <para>
    /// A series lists empty periods too. A chart that skipped the days with nothing on them would
    /// draw a quiet Tuesday as if it never happened, and the eye would read the gap as a trend.
    /// </para>
    /// </summary>
    public static IReadOnlyList<DateOnly> Between(DateOnly first, DateOnly last, ReportGranularity granularity)
    {
        var periods = new List<DateOnly>();

        for (var start = StartOf(first, granularity); start <= last; start = Next(start, granularity))
        {
            periods.Add(start);
        }

        return periods;
    }

    private static DateOnly Next(DateOnly start, ReportGranularity granularity) => granularity switch
    {
        ReportGranularity.Day => start.AddDays(1),
        ReportGranularity.Week => start.AddDays(7),
        _ => start.AddMonths(1),
    };
}
