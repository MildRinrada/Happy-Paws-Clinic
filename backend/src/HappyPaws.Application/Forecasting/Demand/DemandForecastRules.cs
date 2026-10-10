namespace HappyPaws.Application.Forecasting.Demand;

/// <summary>
/// The arithmetic of <c>GET /staff/stats/demand-forecast</c> (<c>docs/contract-extensions.md § 33</c>).
/// Pure, so each rule has a unit test of its own.
/// <list type="bullet">
/// <item><b>Expected walk-ins</b> for a day and hour: the average count of walk-in tickets issued in
/// that hour on the same weekday over the previous <see cref="HistoryWeeks"/> weeks before today.
/// Weeks before the branch issued its first ticket are not counted as zeroes, so a new branch is not
/// forecast as quiet.</item>
/// <item><b>Vets on shift</b> in an hour: vets whose active shift overlaps any part of it.</item>
/// <item><b>High load</b>: expected patients per vet above the branch's own 80th percentile over the
/// same weeks, taken over the hours that had a vet in and at least one patient. With fewer than
/// <see cref="MinimumSamples"/> such hours there is no threshold, and only an hour with patients
/// expected and no vet on shift is flagged.</item>
/// </list>
/// </summary>
public static class DemandForecastRules
{
    public const int HistoryWeeks = 8;

    /// <summary>The longest span one request may forecast.</summary>
    public const int MaxSpanDays = 14;

    /// <summary>The span forecast when <c>to</c> is absent: a week.</summary>
    public const int DefaultSpanDays = 7;

    public const double HighLoadPercentile = 0.8;

    /// <summary>Hours of history needed before a percentile means anything.</summary>
    public const int MinimumSamples = 10;

    /// <summary>The same-weekday days before <paramref name="today"/> that a day's walk-ins are averaged over, newest first.</summary>
    /// <param name="day">The day forecast.</param>
    /// <param name="today">The clinic's today; history stops the day before.</param>
    /// <param name="firstTicketDay">The branch's first ticket, or null when it has none.</param>
    public static IReadOnlyList<DateOnly> SampleDays(DateOnly day, DateOnly today, DateOnly? firstTicketDay)
    {
        if (firstTicketDay is not { } first)
        {
            return [];
        }

        // The latest day before today that falls on the same weekday as the day forecast.
        var back = ((int)today.DayOfWeek - (int)day.DayOfWeek + 7) % 7;
        var latest = today.AddDays(back == 0 ? -7 : -back);

        return [.. Enumerable.Range(0, HistoryWeeks)
            .Select(week => latest.AddDays(-7 * week))
            .Where(sample => sample >= first)];
    }

    /// <summary>The mean count over the sample days, zero when there are none.</summary>
    public static double Average(IReadOnlyList<DateOnly> sampleDays, Func<DateOnly, int> countOn) =>
        sampleDays.Count == 0 ? 0 : sampleDays.Sum(countOn) / (double)sampleDays.Count;

    /// <summary>
    /// Distinct vets on shift in each clinic hour the shifts touch, keyed by day and hour.
    /// </summary>
    public static Dictionary<(DateOnly Day, int Hour), int> VetsByHour(IEnumerable<ShiftSpan> shifts, TimeZoneInfo zone)
    {
        var staff = new Dictionary<(DateOnly, int), HashSet<string>>();

        foreach (var shift in shifts)
        {
            var start = TimeZoneInfo.ConvertTime(shift.StartsAt, zone);
            var end = TimeZoneInfo.ConvertTime(shift.EndsAt, zone);
            var hour = new DateTimeOffset(start.Year, start.Month, start.Day, start.Hour, 0, 0, start.Offset);

            for (; hour < end; hour = hour.AddHours(1))
            {
                var key = (DateOnly.FromDateTime(hour.DateTime), hour.Hour);

                if (!staff.TryGetValue(key, out var vets))
                {
                    staff[key] = vets = new HashSet<string>(StringComparer.Ordinal);
                }

                vets.Add(shift.StaffId);
            }
        }

        return staff.ToDictionary(pair => pair.Key, pair => pair.Value.Count);
    }

    /// <summary>
    /// The value below which <paramref name="percentile"/> of the samples fall, interpolated between
    /// neighbours as PostgreSQL's <c>percentile_cont</c> does; null with no samples.
    /// </summary>
    public static double? Percentile(IReadOnlyCollection<double> samples, double percentile)
    {
        if (samples.Count == 0)
        {
            return null;
        }

        var sorted = samples.Order().ToArray();
        var position = percentile * (sorted.Length - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);

        return sorted[lower] + ((sorted[upper] - sorted[lower]) * (position - lower));
    }

    /// <summary>The high-load line for a branch, or null when its history is too thin to draw one.</summary>
    public static double? Threshold(IReadOnlyCollection<double> loadsPerVet) =>
        loadsPerVet.Count < MinimumSamples ? null : Percentile(loadsPerVet, HighLoadPercentile);

    /// <summary>Expected patients per vet; null when no vet is on shift.</summary>
    public static double? LoadPerVet(double expected, int vets) => vets > 0 ? expected / vets : null;

    /// <summary>
    /// Above the branch's own line, or patients expected (at least one) with no vet on shift.
    /// </summary>
    public static bool IsHighLoad(double expected, int vets, double? threshold) =>
        vets > 0
            ? threshold is { } line && expected / vets > line
            : expected >= 1;

    /// <summary>Two decimals, for the wire.</summary>
    public static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
