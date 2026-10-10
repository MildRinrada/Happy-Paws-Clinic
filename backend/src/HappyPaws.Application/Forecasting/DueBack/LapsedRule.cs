namespace HappyPaws.Application.Forecasting;

/// <summary>
/// When a customer has been away longer than usual <b>for them</b>
/// (<c>docs/contract-extensions.md § 33</c>). A CRM segment's <c>inactive_days</c> is one number for
/// everyone; a dog brought in every month and a cat seen once a year are not late at the same point.
/// <list type="bullet">
/// <item>A <b>visit</b> is a clinic day with a consultation or a bill that was not voided, the same as
/// the segments and the retention report. Two on one day are one visit.</item>
/// <item>Only customers with at least <see cref="MinVisits"/> visits have a rhythm to judge.</item>
/// <item><b>Usual gap</b>: the median of the days between consecutive visits.</item>
/// <item><b>Allowed</b>: the larger of <see cref="Factor"/> × the usual gap and the usual gap plus
/// <see cref="SlackDays"/>, rounded up to whole days. The slack keeps a monthly customer from being
/// called after a few days late; the factor stretches the allowance for the once-a-year ones.</item>
/// <item><b>Due back</b> on the last visit plus the allowance. Lapsed from the day after.</item>
/// </list>
/// The SQL in Infrastructure computes the same thing with <c>percentile_cont(0.5)</c> and these
/// constants; the integration tests hold the two together. The endpoint and the
/// <c>customer_lapsed</c> automation trigger both read that one statement.
/// </summary>
public static class LapsedRule
{
    public const int MinVisits = 3;
    public const double Factor = 1.5;
    public const int SlackDays = 30;

    /// <summary>The median gap between consecutive visit days; null with fewer than two days.</summary>
    public static double? MedianGap(IEnumerable<DateOnly> visitDays)
    {
        var days = visitDays.Distinct().Order().ToArray();

        if (days.Length < 2)
        {
            return null;
        }

        var gaps = days.Zip(days.Skip(1), (earlier, later) => (double)(later.DayNumber - earlier.DayNumber)).Order().ToArray();
        var middle = gaps.Length / 2;

        return gaps.Length % 2 == 1 ? gaps[middle] : (gaps[middle - 1] + gaps[middle]) / 2;
    }

    /// <summary>Days after the last visit before the customer counts as lapsed.</summary>
    public static int AllowedDays(double medianGap) =>
        (int)Math.Ceiling(Math.Max(Factor * medianGap, medianGap + SlackDays) - 1e-9);

    /// <summary>The day they would usually have been back by; null without enough visits.</summary>
    public static DateOnly? DueOn(IEnumerable<DateOnly> visitDays)
    {
        var days = visitDays.Distinct().ToArray();

        if (days.Length < MinVisits || MedianGap(days) is not { } gap)
        {
            return null;
        }

        return days.Max().AddDays(AllowedDays(gap));
    }

    /// <summary>True when today is past the day they were due back.</summary>
    public static bool IsLapsed(IEnumerable<DateOnly> visitDays, DateOnly today) =>
        DueOn(visitDays) is { } due && today > due;
}
