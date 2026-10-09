namespace HappyPaws.Domain.Crm;

/// <summary>Membership levels — "ระดับสมาชิก".</summary>
public enum LoyaltyTier
{
    Member,
    Silver,
    Gold,
}

/// <summary>
/// How points are earned, what they are worth and where the levels start.
/// <para>
/// The contract names points, levels and redemptions but no numbers, so these are the clinic's
/// defaults, kept in one place (<c>docs/assumptions.md</c>, CRM section). There is no endpoint to
/// change them; a clinic that wants other numbers changes them here.
/// </para>
/// </summary>
public static class LoyaltyRules
{
    /// <summary>One point for every ฿100 actually paid on a bill.</summary>
    public const long SatangPerPoint = 10_000;

    /// <summary>One point takes ฿1 off a bill.</summary>
    public const long PointValueSatang = 100;

    /// <summary>The lowest lifetime points for each level, lowest level first.</summary>
    public static IReadOnlyList<(LoyaltyTier Tier, int MinPoints)> Tiers { get; } =
    [
        (LoyaltyTier.Member, 0),
        (LoyaltyTier.Silver, 200),
        (LoyaltyTier.Gold, 1_000),
    ];

    /// <summary>Whole points for an amount, rounded down: ฿199 earns one point, not two.</summary>
    public static int PointsFor(long satang) => satang <= 0 ? 0 : (int)(satang / SatangPerPoint);

    /// <summary>
    /// The level for a customer's lifetime points (earned less reversed, never minus what was
    /// spent). Spending points does not cost a customer their level.
    /// </summary>
    public static LoyaltyTier TierFor(int lifetimePoints) =>
        Tiers.Last(tier => lifetimePoints >= tier.MinPoints || tier.MinPoints == 0).Tier;

    /// <summary>The next level up and how far away it is, or null at the top.</summary>
    public static (LoyaltyTier Tier, int PointsNeeded)? NextTier(int lifetimePoints)
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
