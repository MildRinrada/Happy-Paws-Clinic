namespace HappyPaws.Domain.Crm;

/// <summary>Membership levels — "ระดับสมาชิก".</summary>
public enum LoyaltyTier
{
    Member,
    Silver,
    Gold,
}

/// <summary>
/// The default points programme, and the arithmetic every programme shares.
/// <para>
/// The contract names points, levels and redemptions but no numbers, so these are the clinic's
/// defaults (<c>docs/assumptions.md § 43</c>). A clinic changes them with
/// <c>PUT /clinic/settings/loyalty</c> (<see cref="LoyaltySettings"/>); these values apply until it
/// does, and to earn rows written before the rate was a setting.
/// </para>
/// </summary>
public static class LoyaltyRules
{
    /// <summary>One point for every ฿100 actually paid on a bill.</summary>
    public const long SatangPerPoint = 10_000;

    /// <summary>One point takes ฿1 off a bill.</summary>
    public const long PointValueSatang = 100;

    public const int DefaultSilverMinPoints = 200;
    public const int DefaultGoldMinPoints = 1_000;

    private static readonly LoyaltySettings Defaults = LoyaltySettings.CreateDefault();

    /// <summary>The default lowest lifetime points for each level, lowest level first.</summary>
    public static IReadOnlyList<(LoyaltyTier Tier, int MinPoints)> Tiers => Defaults.Tiers;

    /// <summary>Whole points for an amount, rounded down: ฿199 earns one point at ฿100 a point, not two.</summary>
    public static int PointsFor(long satang, long satangPerPoint = SatangPerPoint) =>
        satang <= 0 || satangPerPoint <= 0 ? 0 : (int)(satang / satangPerPoint);

    /// <summary>The default level for lifetime points (earned less reversed, never minus what was spent).</summary>
    public static LoyaltyTier TierFor(int lifetimePoints) => Defaults.TierFor(lifetimePoints);

    /// <summary>The next default level up and how far away it is, or null at the top.</summary>
    public static (LoyaltyTier Tier, int PointsNeeded)? NextTier(int lifetimePoints) => Defaults.NextTier(lifetimePoints);
}
