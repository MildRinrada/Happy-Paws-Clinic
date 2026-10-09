using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Crm;

/// <summary>
/// The clinic's points programme: how points are earned, what one is worth, where the levels start
/// and whether points expire. Read by <c>GET /clinic/settings/loyalty</c> and replaced by
/// <c>PUT /clinic/settings/loyalty</c>, neither in the API reference
/// (<c>docs/contract-extensions.md § 25</c>). Until the clinic saves its own, the values are the
/// defaults in <see cref="LoyaltyRules"/>.
/// <para>
/// <b>A change applies from now on.</b> Earning uses the rate at the moment a bill is paid and the
/// earn row keeps that rate, so a refund later takes back points at the rate they were earned at.
/// Redeeming uses the point value at the moment of redemption. Expiry is stamped on each row that
/// adds points when it is written. Ledger rows are never rewritten. The level is not stored, so new
/// thresholds re-label every customer at once; that is a label, not money.
/// </para>
/// <para>
/// One row per instance, under a fixed id, like the clinic's other settings.
/// </para>
/// </summary>
public sealed class LoyaltySettings : Entity
{
    public const string SingletonId = "set_loyalty";

    public const long MinSatangPerPoint = 100;
    public const long MaxSatangPerPoint = 10_000_000;
    public const long MaxPointValueSatang = 100_000;
    public const int MaxTierPoints = 10_000_000;
    public const int MaxExpiryMonths = 120;

    private LoyaltySettings()
    {
    }

    /// <summary>How many satang actually paid earn one point. Default ฿100.</summary>
    public long SatangPerPoint { get; private set; } = LoyaltyRules.SatangPerPoint;

    /// <summary>What one point takes off a bill, in satang. Default ฿1.</summary>
    public long PointValueSatang { get; private set; } = LoyaltyRules.PointValueSatang;

    /// <summary>Lifetime points from which a customer is <see cref="LoyaltyTier.Silver"/>.</summary>
    public int SilverMinPoints { get; private set; } = LoyaltyRules.DefaultSilverMinPoints;

    /// <summary>Lifetime points from which a customer is <see cref="LoyaltyTier.Gold"/>.</summary>
    public int GoldMinPoints { get; private set; } = LoyaltyRules.DefaultGoldMinPoints;

    /// <summary>Months after which points given from now on expire, or null for never.</summary>
    public int? PointsExpireAfterMonths { get; private set; }

    /// <summary>The levels and where each starts, lowest first.</summary>
    public IReadOnlyList<(LoyaltyTier Tier, int MinPoints)> Tiers =>
    [
        (LoyaltyTier.Member, 0),
        (LoyaltyTier.Silver, SilverMinPoints),
        (LoyaltyTier.Gold, GoldMinPoints),
    ];

    public static LoyaltySettings CreateDefault() => new() { Id = SingletonId };

    /// <summary>Replaces every value: a <c>PUT</c>.</summary>
    public void Replace(
        long satangPerPoint,
        long pointValueSatang,
        int silverMinPoints,
        int goldMinPoints,
        int? pointsExpireAfterMonths)
    {
        if (satangPerPoint is < MinSatangPerPoint or > MaxSatangPerPoint)
        {
            throw new DomainException("invalid_loyalty_settings", "ยอดชำระต่อหนึ่งแต้มต้องอยู่ระหว่าง ฿1 ถึง ฿100,000");
        }

        if (pointValueSatang is < 1 or > MaxPointValueSatang)
        {
            throw new DomainException("invalid_loyalty_settings", "มูลค่าแต้มต้องอยู่ระหว่าง 1 สตางค์ ถึง ฿1,000");
        }

        if (silverMinPoints < 1 || goldMinPoints <= silverMinPoints || goldMinPoints > MaxTierPoints)
        {
            throw new DomainException("invalid_loyalty_settings", "เกณฑ์ระดับ gold ต้องมากกว่า silver และ silver ต้องอย่างน้อย 1 แต้ม");
        }

        if (pointsExpireAfterMonths is < 1 or > MaxExpiryMonths)
        {
            throw new DomainException("invalid_loyalty_settings", $"อายุแต้มต้องอยู่ระหว่าง 1 ถึง {MaxExpiryMonths} เดือน");
        }

        SatangPerPoint = satangPerPoint;
        PointValueSatang = pointValueSatang;
        SilverMinPoints = silverMinPoints;
        GoldMinPoints = goldMinPoints;
        PointsExpireAfterMonths = pointsExpireAfterMonths;
    }

    /// <summary>Whole points for an amount at today's rate, rounded down.</summary>
    public int PointsFor(long satang) => LoyaltyRules.PointsFor(satang, SatangPerPoint);

    /// <summary>When points given now expire, or null when they do not.</summary>
    public DateTimeOffset? ExpiryFor(DateTimeOffset givenAt) =>
        PointsExpireAfterMonths is { } months ? givenAt.AddMonths(months) : null;

    /// <summary>The level for lifetime points. Spending points does not cost a customer their level.</summary>
    public LoyaltyTier TierFor(int lifetimePoints) =>
        Tiers.Last(tier => lifetimePoints >= tier.MinPoints || tier.MinPoints == 0).Tier;

    /// <summary>The next level up and how far away it is, or null at the top.</summary>
    public (LoyaltyTier Tier, int PointsNeeded)? NextTier(int lifetimePoints)
    {
        foreach (var (tier, minPoints) in Tiers)
        {
            if (minPoints > lifetimePoints)
            {
                return (tier, minPoints - lifetimePoints);
            }
        }

        return null;
    }
}
