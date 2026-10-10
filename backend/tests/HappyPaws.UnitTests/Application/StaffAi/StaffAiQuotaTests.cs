using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.StaffAi;

namespace HappyPaws.UnitTests.Application.StaffAi;

/// <summary>The monthly allowance: counted before the call, refused when spent, refunded to the right month.</summary>
public class StaffAiQuotaTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed class BangkokClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now { get; set; } = now;

        public DateTimeOffset UtcNow => Now.ToUniversalTime();

        public TimeZoneInfo ClinicTimeZone => TimeZoneInfo.CreateCustomTimeZone("ICT", TimeSpan.FromHours(7), "ICT", "ICT");

        public DateTimeOffset NowInClinicTime() => TimeZoneInfo.ConvertTime(Now, ClinicTimeZone);
    }

    /// <summary>The same rule as the SQL: the total is checked, then the feature counted.</summary>
    private sealed class MemoryStore : IStaffAiUsageStore
    {
        public Dictionary<(string Month, string Feature), int> Rows { get; } = [];

        public Task<bool> TryReserveAsync(string month, string feature, int limit, CancellationToken cancellationToken = default)
        {
            var total = Rows.GetValueOrDefault((month, "*"));
            if (total >= limit) return Task.FromResult(false);

            Rows[(month, "*")] = total + 1;
            Rows[(month, feature)] = Rows.GetValueOrDefault((month, feature)) + 1;
            return Task.FromResult(true);
        }

        public Task ReleaseAsync(string month, string feature, CancellationToken cancellationToken = default)
        {
            foreach (var key in new[] { (month, "*"), (month, feature) })
            {
                if (Rows.GetValueOrDefault(key) > 0) Rows[key]--;
            }

            return Task.CompletedTask;
        }

        public Task<StaffAiUsageCounts> ReadAsync(string month, CancellationToken cancellationToken = default) =>
            Task.FromResult(new StaffAiUsageCounts(
                Rows.GetValueOrDefault((month, "*")),
                Rows.Where(row => row.Key.Month == month && row.Key.Feature != "*").ToDictionary(row => row.Key.Feature, row => row.Value)));
    }

    [Fact]
    public async Task The_month_is_spent_then_refused_with_429_and_the_month_and_limit_in_details()
    {
        var store = new MemoryStore();
        var quota = new StaffAiQuota(store, new StaffAiSettings(MonthlyCalls: 2), new BangkokClock(new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.FromHours(7))));

        await quota.ReserveAsync(StaffAiFeatures.PetSummary, Token);
        await quota.ReserveAsync(StaffAiFeatures.Ask, Token);
        var refused = await Assert.ThrowsAsync<AiQuotaExceededException>(() => quota.ReserveAsync(StaffAiFeatures.Ask, Token));

        Assert.Equal(HttpStatus.TooManyRequests, refused.StatusCode);
        Assert.Equal(ErrorCodes.AiQuotaExceeded, refused.Code);
        Assert.Equal("2026-10", refused.Details!["month"]);
        Assert.Equal(2, refused.Details["limit"]);

        var counts = await store.ReadAsync("2026-10", Token);
        Assert.Equal(2, counts.Used);
        Assert.Equal(1, counts.ByFeature[StaffAiFeatures.Ask]);
    }

    [Fact]
    public async Task The_month_is_the_clinic_s_month_not_UTC_s()
    {
        // 18:00 UTC on 31 October is already 1 November in Bangkok.
        var clock = new BangkokClock(new DateTimeOffset(2026, 10, 31, 18, 0, 0, TimeSpan.Zero));
        var quota = new StaffAiQuota(new MemoryStore(), new StaffAiSettings(), clock);

        Assert.Equal("2026-11", quota.CurrentMonth);
        Assert.Equal("2026-11", (await quota.ReserveAsync(StaffAiFeatures.SoapDraft, Token)).Month);
    }

    [Fact]
    public async Task A_refund_goes_back_to_the_month_it_was_taken_from()
    {
        var store = new MemoryStore();
        var clock = new BangkokClock(new DateTimeOffset(2026, 10, 31, 23, 59, 59, TimeSpan.FromHours(7)));
        var quota = new StaffAiQuota(store, new StaffAiSettings(MonthlyCalls: 5), clock);

        var reservation = await quota.ReserveAsync(StaffAiFeatures.OwnerMessage, Token);
        clock.Now = clock.Now.AddSeconds(5);
        await quota.ReserveAsync(StaffAiFeatures.OwnerMessage, Token);
        await quota.RefundAsync(reservation, Token);

        Assert.Equal(0, (await store.ReadAsync("2026-10", Token)).Used);
        Assert.Equal(1, (await store.ReadAsync("2026-11", Token)).Used);
    }

    [Fact]
    public async Task A_new_month_starts_from_zero()
    {
        var store = new MemoryStore();
        var clock = new BangkokClock(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.FromHours(7)));
        var quota = new StaffAiQuota(store, new StaffAiSettings(MonthlyCalls: 1), clock);

        await quota.ReserveAsync(StaffAiFeatures.Ask, Token);
        await Assert.ThrowsAsync<AiQuotaExceededException>(() => quota.ReserveAsync(StaffAiFeatures.Ask, Token));

        clock.Now = clock.Now.AddDays(1);
        await quota.ReserveAsync(StaffAiFeatures.Ask, Token);
    }

    [Fact]
    public async Task No_allowance_refuses_everything()
    {
        var quota = new StaffAiQuota(new MemoryStore(), new StaffAiSettings(MonthlyCalls: 0), new BangkokClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<AiQuotaExceededException>(() => quota.ReserveAsync(StaffAiFeatures.Ask, Token));
    }
}
