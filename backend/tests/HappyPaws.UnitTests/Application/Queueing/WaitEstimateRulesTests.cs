using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Queueing.WaitEstimate;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.UnitTests.Application.Queueing;

/// <summary>The walk-in wait estimate's arithmetic and open check (<c>docs/contract-extensions.md § 37</c>).</summary>
public class WaitEstimateRulesTests
{
    private const string Branch = "br_main";

    // A Saturday, 10:15 clinic time.
    private static readonly DateTime SaturdayMorning = new(2026, 10, 10, 10, 15, 0);

    [Theory]
    [InlineData(3, 20, 2, 30, 40)]
    [InlineData(3, 15, 2, 20, 30)]
    [InlineData(1, 20, 1, 20, 30)]
    [InlineData(2, 4, 1, 0, 10)]
    [InlineData(5, 18, 3, 30, 40)]
    public void The_wait_is_waiting_times_minutes_over_vets_floored_to_ten_with_a_ten_minute_range(
        int waiting, int minutes, int vets, int low, int high)
    {
        Assert.Equal((low, high), WaitEstimateRules.Range(waiting, minutes, vets));
    }

    [Fact]
    public void Nobody_waiting_is_zero_to_ten()
    {
        Assert.Equal((0, 10), WaitEstimateRules.Range(0, 25, 2));
    }

    [Fact]
    public void An_open_branch_with_no_vet_on_the_roster_counts_one()
    {
        Assert.Equal(WaitEstimateRules.Range(2, 20, 1), WaitEstimateRules.Range(2, 20, 0));
    }

    [Fact]
    public void The_branch_s_median_is_used_once_there_are_enough_timed_visits()
    {
        Assert.Equal(17, WaitEstimateRules.ConsultationMinutes(12, 16.5));
        Assert.Equal(WaitEstimateRules.DefaultConsultationMinutes, WaitEstimateRules.ConsultationMinutes(4, 9));
        Assert.Equal(WaitEstimateRules.DefaultConsultationMinutes, WaitEstimateRules.ConsultationMinutes(0, null));
    }

    [Fact]
    public void Open_inside_the_day_s_hours_and_closed_outside_them()
    {
        var hours = Week(new TimeOnly(9, 0), new TimeOnly(18, 0));

        Assert.True(WaitEstimateRules.IsOpen(hours, [], Branch, SaturdayMorning, vetsOnDuty: 0));
        Assert.True(WaitEstimateRules.IsOpen(hours, [], Branch, SaturdayMorning.Date.AddHours(9), 1));
        Assert.False(WaitEstimateRules.IsOpen(hours, [], Branch, SaturdayMorning.Date.AddHours(18), 1));
        Assert.False(WaitEstimateRules.IsOpen(hours, [], Branch, SaturdayMorning.Date.AddHours(8.5), 1));
    }

    [Fact]
    public void A_weekly_closed_day_is_closed()
    {
        var hours = Week(new TimeOnly(9, 0), new TimeOnly(18, 0), closed: DayOfWeek.Saturday);

        Assert.False(WaitEstimateRules.IsOpen(hours, [], Branch, SaturdayMorning, 2));
    }

    [Fact]
    public void A_holiday_shuts_its_own_branch_or_every_branch_but_not_another_branch()
    {
        var hours = Week(new TimeOnly(9, 0), new TimeOnly(18, 0));
        var today = DateOnly.FromDateTime(SaturdayMorning);

        Assert.False(WaitEstimateRules.IsOpen(hours, [ClinicHoliday.Declare(today, "ปิด", Branch)], Branch, SaturdayMorning, 2));
        Assert.False(WaitEstimateRules.IsOpen(hours, [ClinicHoliday.Declare(today, "ปิดทุกสาขา")], Branch, SaturdayMorning, 2));
        Assert.True(WaitEstimateRules.IsOpen(hours, [ClinicHoliday.Declare(today, "ปิด", "br_other")], Branch, SaturdayMorning, 2));
    }

    [Fact]
    public void Hours_never_set_are_judged_by_the_roster()
    {
        var unset = OpeningHours.CreateDefault();

        Assert.True(WaitEstimateRules.IsOpen(unset, [], Branch, SaturdayMorning, 1));
        Assert.False(WaitEstimateRules.IsOpen(unset, [], Branch, SaturdayMorning, 0));
        Assert.Null(WaitEstimateRules.NextOpening(unset, [], Branch, SaturdayMorning));
    }

    [Fact]
    public void A_closed_branch_opens_next_later_today_or_on_the_next_open_day_that_no_holiday_shuts()
    {
        var hours = Week(new TimeOnly(9, 0), new TimeOnly(18, 0), closed: DayOfWeek.Sunday);

        Assert.Equal(
            SaturdayMorning.Date.AddHours(9),
            WaitEstimateRules.NextOpening(hours, [], Branch, SaturdayMorning.Date.AddHours(7)));

        // Saturday evening: Sunday is closed, so Monday 09:00.
        Assert.Equal(
            new DateTime(2026, 10, 12, 9, 0, 0),
            WaitEstimateRules.NextOpening(hours, [], Branch, SaturdayMorning.Date.AddHours(19)));

        // And with Monday a holiday at this branch, Tuesday.
        Assert.Equal(
            new DateTime(2026, 10, 13, 9, 0, 0),
            WaitEstimateRules.NextOpening(
                hours, [ClinicHoliday.Declare(new DateOnly(2026, 10, 12), "หยุด", Branch)], Branch, SaturdayMorning.Date.AddHours(19)));
    }

    [Fact]
    public void An_answer_is_reused_for_thirty_seconds_and_then_worked_out_again()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 10, 10, 3, 0, 0, TimeSpan.Zero));
        var cache = new WaitEstimateCache(clock);
        var estimate = new WaitEstimateDto(Branch, true, 2, 1, 40, 50, null, clock.UtcNow);

        cache.Store(estimate);

        clock.Now = clock.Now.AddSeconds(29);
        Assert.Same(estimate, cache.Fresh(Branch));
        Assert.Null(cache.Fresh("br_other"));

        clock.Now = clock.Now.AddSeconds(1);
        Assert.Null(cache.Fresh(Branch));
    }

    private static OpeningHours Week(TimeOnly opens, TimeOnly closes, DayOfWeek? closed = null)
    {
        var hours = OpeningHours.CreateDefault();

        hours.ReplaceWeek(Enum.GetValues<DayOfWeek>().Select(day =>
            day == closed ? DayHours.Closed(day) : new DayHours(day, opens, closes)));

        return hours;
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now { get; set; } = now;

        public DateTimeOffset UtcNow => Now.ToUniversalTime();

        public TimeZoneInfo ClinicTimeZone => TimeZoneInfo.Utc;

        public DateTimeOffset NowInClinicTime() => Now;
    }
}
