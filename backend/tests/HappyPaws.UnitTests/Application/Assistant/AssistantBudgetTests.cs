using HappyPaws.Application.Assistant;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.UnitTests.Application.Assistant;

/// <summary>The daily ceiling on model calls: the line between a clinic's package and its bill.</summary>
public class AssistantBudgetTests
{
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now { get; set; } = now;

        public DateTimeOffset UtcNow => Now.ToUniversalTime();

        public TimeZoneInfo ClinicTimeZone => TimeZoneInfo.Utc;

        public DateTimeOffset NowInClinicTime() => Now;
    }

    [Fact]
    public void The_day_s_allowance_is_spent_then_refused_then_renewed_at_midnight()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 10, 3, 23, 0, 0, TimeSpan.FromHours(7)));
        var budget = new AssistantBudget(new AssistantSettings(DailyConversationTurns: 2), clock);

        budget.Take();
        budget.Take();
        var refused = Assert.Throws<RateLimitedException>(budget.Take);
        Assert.Equal(HttpStatus.TooManyRequests, refused.StatusCode);

        clock.Now = clock.Now.AddHours(2);
        budget.Take();
    }

    [Fact]
    public void Zero_means_no_ceiling()
    {
        var budget = new AssistantBudget(new AssistantSettings(DailyConversationTurns: 0), new FixedClock(DateTimeOffset.UtcNow));

        for (var i = 0; i < 10_000; i++) budget.Take();
    }
}
