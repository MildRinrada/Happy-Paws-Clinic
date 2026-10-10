using HappyPaws.Application.Forecasting;
using HappyPaws.Application.Forecasting.Demand;

namespace HappyPaws.UnitTests.Application.Forecasting;

/// <summary>Expected walk-ins, vets per hour and the high-load line (<c>docs/contract-extensions.md § 33</c>).</summary>
public class DemandForecastRulesTests
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    // A Saturday.
    private static readonly DateOnly Today = new(2026, 10, 10);

    [Fact]
    public void Walk_ins_are_averaged_over_the_same_weekday_of_the_eight_weeks_before_today()
    {
        var monday = new DateOnly(2026, 10, 12);

        var samples = DemandForecastRules.SampleDays(monday, Today, firstTicketDay: new DateOnly(2020, 1, 1));

        Assert.Equal(8, samples.Count);
        Assert.All(samples, day => Assert.Equal(DayOfWeek.Monday, day.DayOfWeek));
        Assert.Equal(new DateOnly(2026, 10, 5), samples[0]);
        Assert.Equal(new DateOnly(2026, 8, 17), samples[^1]);
        Assert.All(samples, day => Assert.True(day < Today));
    }

    [Fact]
    public void Today_is_not_its_own_history()
    {
        var samples = DemandForecastRules.SampleDays(Today, Today, new DateOnly(2020, 1, 1));

        Assert.Equal(Today.AddDays(-7), samples[0]);
    }

    [Fact]
    public void Weeks_before_the_branch_issued_its_first_ticket_are_not_counted_as_quiet()
    {
        var monday = new DateOnly(2026, 10, 12);

        var samples = DemandForecastRules.SampleDays(monday, Today, firstTicketDay: new DateOnly(2026, 9, 20));

        Assert.Equal([new DateOnly(2026, 10, 5), new DateOnly(2026, 9, 28), new DateOnly(2026, 9, 21)], samples);
        Assert.Empty(DemandForecastRules.SampleDays(monday, Today, firstTicketDay: null));
    }

    [Fact]
    public void The_expected_count_is_the_mean_of_the_sample_days()
    {
        DateOnly[] days = [new(2026, 10, 5), new(2026, 9, 28), new(2026, 9, 21), new(2026, 9, 14)];
        var counts = new Dictionary<DateOnly, int> { [days[0]] = 3, [days[1]] = 1, [days[3]] = 2 };

        Assert.Equal(1.5, DemandForecastRules.Average(days, day => counts.GetValueOrDefault(day)));
        Assert.Equal(0, DemandForecastRules.Average([], _ => 5));
    }

    [Fact]
    public void A_vet_is_on_shift_in_every_clinic_hour_their_shift_touches_and_counts_once()
    {
        var day = new DateOnly(2026, 10, 12);
        DateTimeOffset At(int hour, int minute = 0) => new(day.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.FromHours(7));

        var vets = DemandForecastRules.VetsByHour(
            [
                new ShiftSpan("stf_a", At(9), At(12)),
                new ShiftSpan("stf_a", At(11), At(13)),
                new ShiftSpan("stf_b", At(10, 30), At(11, 15)),
            ],
            Bangkok);

        Assert.Equal(1, vets[(day, 9)]);
        Assert.Equal(2, vets[(day, 10)]);
        Assert.Equal(2, vets[(day, 11)]);
        Assert.Equal(1, vets[(day, 12)]);
        Assert.False(vets.ContainsKey((day, 13)));
        Assert.False(vets.ContainsKey((day, 8)));
    }

    [Fact]
    public void Hours_are_clinic_hours_whatever_offset_the_shift_was_stored_in()
    {
        var day = new DateOnly(2026, 10, 12);

        // 02:00 UTC is 09:00 in Bangkok.
        var vets = DemandForecastRules.VetsByHour(
            [new ShiftSpan("stf_a", new DateTimeOffset(2026, 10, 12, 2, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 12, 3, 0, 0, TimeSpan.Zero))],
            Bangkok);

        Assert.Equal(1, vets[(day, 9)]);
        Assert.Single(vets);
    }

    [Fact]
    public void The_percentile_interpolates_between_neighbours_like_percentile_cont()
    {
        double[] loads = [1, 2, 3, 4, 5];

        Assert.Equal(4.2, DemandForecastRules.Percentile(loads, 0.8)!.Value, precision: 10);
        Assert.Equal(3, DemandForecastRules.Percentile(loads, 0.5));
        Assert.Equal(7, DemandForecastRules.Percentile([7], 0.8));
        Assert.Null(DemandForecastRules.Percentile([], 0.8));
    }

    [Fact]
    public void Thin_history_draws_no_line()
    {
        Assert.Null(DemandForecastRules.Threshold([1, 2, 3]));

        var ten = Enumerable.Range(1, 10).Select(value => (double)value).ToArray();
        Assert.Equal(8.2, DemandForecastRules.Threshold(ten)!.Value, precision: 10);
    }

    [Fact]
    public void An_hour_is_high_load_above_the_line_or_with_patients_and_no_vet()
    {
        Assert.True(DemandForecastRules.IsHighLoad(expected: 9, vets: 2, threshold: 4));
        Assert.False(DemandForecastRules.IsHighLoad(expected: 8, vets: 2, threshold: 4));
        Assert.False(DemandForecastRules.IsHighLoad(expected: 9, vets: 2, threshold: null));

        Assert.True(DemandForecastRules.IsHighLoad(expected: 1, vets: 0, threshold: null));
        Assert.False(DemandForecastRules.IsHighLoad(expected: 0.4, vets: 0, threshold: 4));
    }

    [Fact]
    public void Load_per_vet_is_absent_with_no_vet()
    {
        Assert.Equal(2.5, DemandForecastRules.LoadPerVet(5, 2));
        Assert.Null(DemandForecastRules.LoadPerVet(5, 0));
    }
}
