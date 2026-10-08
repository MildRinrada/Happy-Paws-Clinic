using HappyPaws.Domain.Analytics;

namespace HappyPaws.UnitTests.Domain.Analytics;

/// <summary>Grouping clinic days into the rows of a report's series.</summary>
public class ReportPeriodsTests
{
    [Theory]
    [InlineData("2026-10-04", "2026-09-28")] // Sunday belongs to the week that began on Monday
    [InlineData("2026-09-28", "2026-09-28")] // Monday starts its own week
    [InlineData("2026-10-01", "2026-09-28")]
    public void A_week_starts_on_monday(string day, string monday) =>
        Assert.Equal(DateOnly.Parse(monday), ReportPeriods.StartOf(DateOnly.Parse(day), ReportGranularity.Week));

    [Fact]
    public void A_month_starts_on_its_first_day() =>
        Assert.Equal(new DateOnly(2026, 2, 1), ReportPeriods.StartOf(new DateOnly(2026, 2, 28), ReportGranularity.Month));

    [Fact]
    public void A_daily_series_lists_every_day_including_the_empty_ones()
    {
        var days = ReportPeriods.Between(new DateOnly(2026, 9, 29), new DateOnly(2026, 10, 2), ReportGranularity.Day);

        Assert.Equal(["2026-09-29", "2026-09-30", "2026-10-01", "2026-10-02"], days.Select(day => day.ToString("yyyy-MM-dd")));
    }

    [Fact]
    public void A_weekly_series_begins_with_the_week_the_first_day_falls_in()
    {
        var weeks = ReportPeriods.Between(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 12), ReportGranularity.Week);

        Assert.Equal([new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 12)], weeks);
    }

    [Fact]
    public void A_monthly_series_crosses_the_year()
    {
        var months = ReportPeriods.Between(new DateOnly(2025, 11, 15), new DateOnly(2026, 1, 3), ReportGranularity.Month);

        Assert.Equal([new DateOnly(2025, 11, 1), new DateOnly(2025, 12, 1), new DateOnly(2026, 1, 1)], months);
    }

    [Fact]
    public void A_rate_over_nothing_is_absent_not_zero()
    {
        Assert.Null(ReportMath.Rate(0, 0));
        Assert.Equal(0.3333m, ReportMath.Rate(1, 3));
        Assert.Equal(1m, ReportMath.Rate(4, 4));
    }

    [Fact]
    public void An_average_duration_is_in_minutes_to_one_place_and_absent_without_samples()
    {
        Assert.Null(ReportMath.AverageMinutes(600, 0));
        Assert.Equal(7.5m, ReportMath.AverageMinutes(900, 2));
        Assert.Equal(0m, ReportMath.AverageMinutes(-30, 1));
    }
}
