namespace HappyPaws.Domain.Analytics;

/// <summary>
/// The two derived numbers every report repeats: a share, and an average duration.
/// <para>
/// Both answer null rather than zero when there is nothing to divide by. "No-show rate 0%" on a
/// day with no appointments says the clinic did well; null says there was nothing to measure, which
/// is the truth.
/// </para>
/// </summary>
public static class ReportMath
{
    /// <summary><paramref name="part"/> as a fraction of <paramref name="whole"/>, to four places.</summary>
    public static decimal? Rate(long part, long whole) =>
        whole <= 0 ? null : Math.Round((decimal)part / whole, 4, MidpointRounding.AwayFromZero);

    /// <summary>The mean of <paramref name="count"/> durations totalling <paramref name="totalSeconds"/>, in minutes to one place.</summary>
    public static decimal? AverageMinutes(double totalSeconds, long count) =>
        count <= 0
            ? null
            : Math.Round((decimal)(Math.Max(0, totalSeconds) / count / 60d), 1, MidpointRounding.AwayFromZero);
}
