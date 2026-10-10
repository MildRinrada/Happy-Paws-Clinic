using HappyPaws.Application.Forecasting;

namespace HappyPaws.UnitTests.Application.Forecasting;

/// <summary>Away longer than usual for them (<c>docs/contract-extensions.md § 33</c>).</summary>
public class LapsedRuleTests
{
    private static readonly DateOnly Start = new(2026, 1, 1);

    private static DateOnly[] Visits(params int[] daysFromStart) => [.. daysFromStart.Select(Start.AddDays)];

    [Fact]
    public void The_usual_gap_is_the_median_of_the_gaps_between_visits()
    {
        // Gaps 30, 10, 50: median 30.
        Assert.Equal(30, LapsedRule.MedianGap(Visits(0, 30, 40, 90)));

        // Gaps 30, 10: the median of an even count is the mean of the middle two.
        Assert.Equal(20, LapsedRule.MedianGap(Visits(0, 30, 40)));
    }

    [Fact]
    public void Two_visits_on_one_day_are_one_visit()
    {
        Assert.Equal(30, LapsedRule.MedianGap(Visits(0, 0, 30, 60)));
        Assert.Null(LapsedRule.DueOn(Visits(0, 0, 30)));
    }

    [Theory]
    [InlineData(20, 50)]
    [InlineData(60, 90)]
    [InlineData(61, 92)]
    [InlineData(365, 548)]
    public void Allowed_days_are_the_larger_of_half_again_and_a_month_more(double medianGap, int expected)
    {
        // 20: 20 + 30 beats 1.5 x 20. 60: the two meet. 61: 1.5 x 61 = 91.5, rounded up.
        // 365: 1.5 x 365 = 547.5, rounded up.
        Assert.Equal(expected, LapsedRule.AllowedDays(medianGap));
    }

    [Fact]
    public void Fewer_than_three_visits_have_no_rhythm_to_judge()
    {
        Assert.Null(LapsedRule.DueOn(Visits(0, 30)));
        Assert.False(LapsedRule.IsLapsed(Visits(0, 30), Start.AddDays(1000)));
    }

    [Fact]
    public void Lapsed_from_the_day_after_the_due_back_day()
    {
        // Monthly visits: median 30, allowed 60, last visit on day 60, due back by day 120.
        var visits = Visits(0, 30, 60);

        Assert.Equal(Start.AddDays(120), LapsedRule.DueOn(visits));
        Assert.False(LapsedRule.IsLapsed(visits, Start.AddDays(120)));
        Assert.True(LapsedRule.IsLapsed(visits, Start.AddDays(121)));
    }

    [Fact]
    public void The_same_absence_is_late_for_one_customer_and_on_time_for_another()
    {
        var today = Start.AddDays(800);
        var monthly = Visits(600, 630, 660);
        var yearly = Visits(0, 365, 730);

        Assert.True(LapsedRule.IsLapsed(monthly, today));
        Assert.False(LapsedRule.IsLapsed(yearly, today));
    }
}
