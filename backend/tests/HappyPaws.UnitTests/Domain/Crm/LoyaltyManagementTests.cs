using HappyPaws.Domain.Common;
using HappyPaws.Domain.Crm;

namespace HappyPaws.UnitTests.Domain.Crm;

/// <summary>
/// <c>docs/contract-extensions.md § 25</c>: the points programme as a setting, expiry, adjustments by
/// hand, and switching a coupon off.
/// </summary>
public class LoyaltyManagementTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(7));

    private static LoyaltyEntry Entry(LoyaltyEntryKind kind, int points, int day, int? expiresAfterDays = null) =>
        LoyaltyEntry.Create(
            "cus_1",
            kind,
            points,
            Start.AddDays(day),
            expiresAt: expiresAfterDays is { } days ? Start.AddDays(day + days) : null);

    [Fact]
    public void The_default_programme_is_the_one_that_was_written_in_code()
    {
        var defaults = LoyaltySettings.CreateDefault();

        Assert.Equal(LoyaltyRules.SatangPerPoint, defaults.SatangPerPoint);
        Assert.Equal(LoyaltyRules.PointValueSatang, defaults.PointValueSatang);
        Assert.Equal(200, defaults.SilverMinPoints);
        Assert.Equal(1_000, defaults.GoldMinPoints);
        Assert.Null(defaults.PointsExpireAfterMonths);
        Assert.Null(defaults.ExpiryFor(Start));
    }

    [Fact]
    public void A_new_programme_changes_the_rate_and_the_levels()
    {
        var settings = LoyaltySettings.CreateDefault();
        settings.Replace(5_000, 200, 100, 500, 12);

        Assert.Equal(29, settings.PointsFor(146_000));
        Assert.Equal(LoyaltyTier.Silver, settings.TierFor(100));
        Assert.Equal(LoyaltyTier.Gold, settings.TierFor(500));
        Assert.Equal((LoyaltyTier.Gold, 1), settings.NextTier(499));
        Assert.Equal(Start.AddMonths(12), settings.ExpiryFor(Start));
    }

    [Theory]
    [InlineData(50, 100, 200, 1_000, null)]
    [InlineData(10_000, 0, 200, 1_000, null)]
    [InlineData(10_000, 100, 0, 1_000, null)]
    [InlineData(10_000, 100, 500, 500, null)]
    [InlineData(10_000, 100, 200, 1_000, 0)]
    public void A_programme_that_makes_no_sense_is_refused(long rate, long value, int silver, int gold, int? months) =>
        Assert.Throws<DomainException>(() => LoyaltySettings.CreateDefault().Replace(rate, value, silver, gold, months));

    [Fact]
    public void A_refund_takes_back_points_at_the_rate_they_were_earned_at()
    {
        // Earned at ฿100 a point; the clinic now gives a point per ฿50. ฿1,460 refunded ฿500 keeps 9.
        Assert.Equal(9, LoyaltyRules.PointsFor(146_000 - 50_000, 10_000));
        Assert.Equal(19, LoyaltyRules.PointsFor(146_000 - 50_000, 5_000));
    }

    [Fact]
    public void Without_expiry_the_balance_is_the_sum_of_the_rows()
    {
        var position = LoyaltyBalance.Compute(
            [Entry(LoyaltyEntryKind.Earn, 100, 0), Entry(LoyaltyEntryKind.Redeem, 30, 1), Entry(LoyaltyEntryKind.Reverse, 10, 2)],
            Start.AddDays(400));

        Assert.Equal(60, position.Balance);
        Assert.Equal(90, position.Lifetime);
        Assert.Equal(0, position.Expired);
        Assert.Null(position.NextExpiryAt);
    }

    [Fact]
    public void Points_left_unspent_past_their_expiry_stop_counting_but_keep_the_level()
    {
        var entries = new[]
        {
            Entry(LoyaltyEntryKind.Earn, 100, 0, expiresAfterDays: 30),
            Entry(LoyaltyEntryKind.Redeem, 40, 10),
            Entry(LoyaltyEntryKind.Earn, 50, 20, expiresAfterDays: 30),
        };

        var before = LoyaltyBalance.Compute(entries, Start.AddDays(25));
        var after = LoyaltyBalance.Compute(entries, Start.AddDays(35));

        Assert.Equal(110, before.Balance);
        Assert.Equal(60, before.NextExpiringPoints);
        Assert.Equal(Start.AddDays(30), before.NextExpiryAt);

        Assert.Equal(50, after.Balance);
        Assert.Equal(60, after.Expired);
        Assert.Equal(150, after.Lifetime);
    }

    [Fact]
    public void Spending_uses_the_points_that_expire_soonest_and_cannot_use_points_already_expired()
    {
        var entries = new[]
        {
            Entry(LoyaltyEntryKind.Earn, 50, 0),
            Entry(LoyaltyEntryKind.Earn, 50, 1, expiresAfterDays: 10),
            Entry(LoyaltyEntryKind.Redeem, 50, 2),
        };

        var position = LoyaltyBalance.Compute(entries, Start.AddDays(100));

        // The expiring parcel was spent first, so the 50 that never expire are left.
        Assert.Equal(50, position.Balance);
        Assert.Equal(0, position.Expired);
    }

    [Fact]
    public void A_refund_of_points_already_spent_leaves_a_debt_the_next_points_settle()
    {
        var entries = new[]
        {
            Entry(LoyaltyEntryKind.Earn, 20, 0),
            Entry(LoyaltyEntryKind.Redeem, 20, 1),
            Entry(LoyaltyEntryKind.Reverse, 10, 2),
        };

        Assert.Equal(-10, LoyaltyBalance.Compute(entries, Start.AddDays(3)).Balance);
        Assert.Equal(5, LoyaltyBalance.Compute([.. entries, Entry(LoyaltyEntryKind.Earn, 15, 4)], Start.AddDays(5)).Balance);
    }

    [Fact]
    public void An_adjustment_keeps_its_sign_and_moves_only_the_balance()
    {
        var give = LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Adjust, 25, Start, note: "ชดเชย", staffId: "stf_1");
        var take = LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Adjust, -5, Start.AddDays(1), note: "แก้ไข", staffId: "stf_1");

        Assert.Equal(25, give.Points);
        Assert.Equal(-5, take.Points);

        var position = LoyaltyBalance.Compute([Entry(LoyaltyEntryKind.Earn, 10, 0), give, take], Start.AddDays(2));

        Assert.Equal(30, position.Balance);
        Assert.Equal(10, position.Lifetime);
    }

    [Fact]
    public void Only_rows_that_add_points_carry_an_expiry_and_only_earn_rows_a_rate()
    {
        var redeem = LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Redeem, 10, Start, expiresAt: Start.AddDays(1), satangPerPoint: 10_000);
        var earn = LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Earn, 10, Start, expiresAt: Start.AddDays(1), satangPerPoint: 10_000);

        Assert.Null(redeem.ExpiresAt);
        Assert.Null(redeem.SatangPerPoint);
        Assert.Equal(Start.AddDays(1), earn.ExpiresAt);
        Assert.Equal(10_000, earn.SatangPerPoint);
    }

    [Fact]
    public void A_switched_off_coupon_is_refused_before_any_other_limit_and_cannot_be_switched_off_twice()
    {
        var coupon = Coupon.Create("OFF10", "ลด 10%", 10, null, [], 1, null, new DateOnly(2026, 1, 1), null, null, "stf_owner");
        var today = new DateOnly(2026, 1, 5);

        Assert.Equal(CouponStatus.Active, coupon.StatusOn(today));

        coupon.Deactivate("stf_owner", "  หลุดไปในแชต ", Start);

        Assert.Equal(CouponStatus.Deactivated, coupon.StatusOn(today));
        Assert.Equal("หลุดไปในแชต", coupon.DeactivationReason);
        Assert.Equal("coupon_deactivated", Assert.Throws<DomainException>(() => coupon.EnsureRedeemable(today, "brn_1", 0)).Code);
        Assert.Equal("state_conflict", Assert.Throws<StateConflictException>(() => coupon.Deactivate("stf_owner", null, Start)).Code);
    }

    [Fact]
    public void A_coupons_status_follows_its_dates_and_count()
    {
        var coupon = Coupon.Create("ONCE", "ครั้งเดียว", null, 5_000, [], 1, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28), null, "stf_owner");

        Assert.Equal(CouponStatus.Scheduled, coupon.StatusOn(new DateOnly(2026, 1, 31)));
        Assert.Equal(CouponStatus.Active, coupon.StatusOn(new DateOnly(2026, 2, 1)));

        coupon.Redeem();

        Assert.Equal(CouponStatus.UsedUp, coupon.StatusOn(new DateOnly(2026, 2, 10)));
        Assert.Equal(CouponStatus.Expired, coupon.StatusOn(new DateOnly(2026, 3, 1)));
    }
}
