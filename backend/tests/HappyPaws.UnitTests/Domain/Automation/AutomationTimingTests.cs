using HappyPaws.Domain.Automation;

namespace HappyPaws.UnitTests.Domain.Automation;

/// <summary>When a rule sends, in the clinic's own time.</summary>
public class AutomationTimingTests
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    [Fact]
    public void Fourteen_days_before_at_nine_is_nine_in_Bangkok()
    {
        var timing = new AutomationTiming(14, null, new TimeOnly(9, 0));

        var sendAt = timing.SendAt(null, new DateOnly(2026, 10, 20), Bangkok);

        Assert.Equal(new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.FromHours(7)), sendAt);
        Assert.Equal(new DateTimeOffset(2026, 10, 6, 2, 0, 0, TimeSpan.Zero), sendAt.ToUniversalTime());
    }

    [Fact]
    public void Twenty_four_hours_before_counts_back_from_the_start()
    {
        var startsAt = new DateTimeOffset(2026, 10, 8, 10, 30, 0, TimeSpan.FromHours(7));

        Assert.Equal(startsAt.AddHours(-24), new AutomationTiming(null, 24, null).SendAt(startsAt, new DateOnly(2026, 10, 8), Bangkok));
    }

    [Fact]
    public void The_morning_of_is_zero_days_at_a_time()
    {
        var sendAt = new AutomationTiming(0, null, new TimeOnly(8, 0)).SendAt(null, new DateOnly(2026, 10, 8), Bangkok);

        Assert.Equal(new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.FromHours(7)), sendAt);
    }

    [Fact]
    public void A_negative_offset_is_after_the_day()
    {
        var sendAt = new AutomationTiming(-7, null, new TimeOnly(9, 0)).SendAt(null, new DateOnly(2026, 10, 1), Bangkok);

        Assert.Equal(new DateOnly(2026, 10, 8), DateOnly.FromDateTime(sendAt.DateTime));
    }

    [Fact]
    public void The_subject_span_covers_every_send_time_in_the_window()
    {
        var from = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddHours(12);

        var (hourFrom, hourTo) = new AutomationTiming(null, 24, null).SubjectSpan(from, to);
        Assert.Equal(from.AddHours(24), hourFrom);
        Assert.Equal(to.AddHours(24), hourTo);

        var (dayFrom, dayTo) = new AutomationTiming(14, null, null).SubjectSpan(from, to);
        Assert.True(dayFrom <= from.AddDays(14) && dayTo >= to.AddDays(14));
    }
}
