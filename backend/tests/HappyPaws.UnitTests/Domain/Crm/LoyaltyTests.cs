using HappyPaws.Domain.Common;
using HappyPaws.Domain.Crm;

namespace HappyPaws.UnitTests.Domain.Crm;

/// <summary>"แต้มสะสม ระดับสมาชิก และประวัติแลก": the earning rate, the levels and the ledger's signs.</summary>
public class LoyaltyTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 9, 10, 0, 0, TimeSpan.FromHours(7));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9_999, 0)]
    [InlineData(10_000, 1)]
    [InlineData(19_999, 1)]
    [InlineData(146_000, 14)]
    public void One_point_per_hundred_baht_rounded_down(long satang, int points) =>
        Assert.Equal(points, LoyaltyRules.PointsFor(satang));

    [Theory]
    [InlineData(0, LoyaltyTier.Member)]
    [InlineData(199, LoyaltyTier.Member)]
    [InlineData(200, LoyaltyTier.Silver)]
    [InlineData(999, LoyaltyTier.Silver)]
    [InlineData(1_000, LoyaltyTier.Gold)]
    public void Levels_follow_lifetime_points(int lifetime, LoyaltyTier tier) =>
        Assert.Equal(tier, LoyaltyRules.TierFor(lifetime));

    [Fact]
    public void The_next_level_says_how_far_away_it_is_and_the_top_has_none()
    {
        Assert.Equal((LoyaltyTier.Silver, 150), LoyaltyRules.NextTier(50));
        Assert.Equal((LoyaltyTier.Gold, 1), LoyaltyRules.NextTier(999));
        Assert.Null(LoyaltyRules.NextTier(1_000));
    }

    [Fact]
    public void The_ledger_signs_points_by_kind_whatever_the_caller_passes()
    {
        Assert.Equal(10, LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Earn, 10, At).Points);
        Assert.Equal(-10, LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Reverse, 10, At).Points);
        Assert.Equal(-10, LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Redeem, -10, At).Points);
        Assert.Equal(10, LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Restore, 10, At).Points);
        Assert.Throws<DomainException>(() => LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Earn, 0, At));
    }

    [Fact]
    public void A_segment_needs_a_condition_and_consistent_day_ranges()
    {
        SegmentCriteria Criteria(int? inactive = null, int? within = null, IReadOnlyList<string>? species = null) =>
            new(species ?? [], inactive, within, null, [], null, null);

        Assert.Throws<DomainException>(() => Segment.Create("ว่าง", Criteria(), "stf_1"));
        Assert.Throws<DomainException>(() => Segment.Create("ขัดกัน", Criteria(inactive: 180, within: 90), "stf_1"));

        var segment = Segment.Create("หมาแมวที่ไม่มา", Criteria(inactive: 180, species: ["สุนัข", " สุนัข ", "แมว"]), "stf_1");

        Assert.Equal(["สุนัข", "แมว"], segment.Species);
        Assert.Equal(180, segment.Criteria.InactiveDays);
    }
}
