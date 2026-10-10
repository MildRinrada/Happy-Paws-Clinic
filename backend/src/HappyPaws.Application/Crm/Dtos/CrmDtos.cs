using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.Dtos;

/// <summary>
/// A package and what is left of it, the same shape for the staff and the owner.
/// <c>status</c> is <c>pending_payment</c>, <c>active</c>, <c>used_up</c>, <c>expired</c> or
/// <c>cancelled</c>.
/// </summary>
public sealed record PackageDto(
    string Id,
    string Name,
    string CustomerId,
    string? PetId,
    IReadOnlyList<string> ServiceIds,
    int TotalUses,
    int UsedCount,
    int Remaining,
    long PriceSatang,
    string? BranchId,
    string? InvoiceId,
    DateTimeOffset PurchasedAt,
    DateOnly? ExpiresOn,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? CancelledAt,
    string Status)
{
    public static PackageDto From(CustomerPackage package, DateOnly today) =>
        new(
            package.Id,
            package.Name,
            package.CustomerId,
            package.PetId,
            package.ServiceIds,
            package.TotalUses,
            package.UsedCount,
            Math.Max(package.Remaining, 0),
            package.PriceSatang,
            package.BranchId,
            package.InvoiceId,
            package.PurchasedAt,
            package.ExpiresOn,
            package.ActivatedAt,
            package.CancelledAt,
            CrmWire.ToWire(package.StatusOn(today)));

    /// <summary>Usable first, then waiting for payment, then the rest; soonest to expire first within each.</summary>
    public static IReadOnlyList<PackageDto> Ordered(IEnumerable<CustomerPackage> packages, DateOnly today) =>
    [
        .. packages
            .Select(package => From(package, today))
            .OrderBy(dto => dto.Status switch { "active" => 0, "pending_payment" => 1, _ => 2 })
            .ThenBy(dto => dto.ExpiresOn ?? DateOnly.MaxValue)
            .ThenByDescending(dto => dto.PurchasedAt),
    ];
}

/// <summary><c>POST /staff/packages</c>: the package and the bill it was sold on.</summary>
public sealed record PackageSaleDto(PackageDto Package, InvoiceDto Invoice);

/// <summary>
/// A coupon. <c>status</c> is <c>active</c>, <c>scheduled</c>, <c>used_up</c>, <c>expired</c> or
/// <c>deactivated</c>; <c>redeemed_count</c> is uses still standing (a voided bill gives its use
/// back) and <c>remaining</c> what is left of <c>max_redemptions</c>, null when unlimited. The status
/// and the switch-off fields are not in the API reference (<c>docs/contract-extensions.md § 25</c>).
/// </summary>
public sealed record CouponDto(
    string Id,
    string Code,
    string Name,
    decimal? Percent,
    long? AmountSatang,
    IReadOnlyList<string> ServiceIds,
    int? MaxRedemptions,
    int? PerCustomerLimit,
    DateOnly ValidFrom,
    DateOnly? ValidUntil,
    string? BranchId,
    int RedeemedCount,
    int? Remaining,
    string Status,
    string CreatedByStaffId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeactivatedAt,
    string? DeactivatedByStaffId,
    string? DeactivationReason)
{
    public static CouponDto From(Coupon coupon, DateOnly today) =>
        new(
            coupon.Id,
            coupon.Code,
            coupon.Name,
            coupon.Percent,
            coupon.AmountSatang,
            coupon.ServiceIds,
            coupon.MaxRedemptions,
            coupon.PerCustomerLimit,
            coupon.ValidFrom,
            coupon.ValidUntil,
            coupon.BranchId,
            coupon.RedeemedCount,
            coupon.MaxRedemptions is { } max ? Math.Max(max - coupon.RedeemedCount, 0) : null,
            CrmWire.ToWire(coupon.StatusOn(today)),
            coupon.CreatedByStaffId,
            coupon.CreatedAt,
            coupon.DeactivatedAt,
            coupon.DeactivatedByStaffId,
            coupon.DeactivationReason);
}

public sealed record SegmentCriteriaDto(
    IReadOnlyList<string> Species,
    int? InactiveDays,
    int? VisitedWithinDays,
    long? MinSpendSatang,
    IReadOnlyList<string> Tiers,
    bool? HasActivePackage,
    string? BranchId)
{
    public static SegmentCriteriaDto From(SegmentCriteria criteria) =>
        new(
            criteria.Species,
            criteria.InactiveDays,
            criteria.VisitedWithinDays,
            criteria.MinSpendSatang,
            [.. criteria.Tiers.Select(CrmWire.ToWire)],
            criteria.HasActivePackage,
            criteria.BranchId);
}

/// <summary>
/// A saved customer group. <c>customer_count</c> is who matched at <c>counted_at</c>;
/// <c>reachable_count</c> is how many of them agreed to marketing and have a LINE address, which is
/// who a broadcast would reach. The answer of <c>POST</c> and the rows of the list carry the counts
/// taken when the group was saved; <c>GET /staff/segments/{id}</c> counts again
/// (<c>counted_at</c> is § 25, not in the API reference).
/// </summary>
public sealed record SegmentDto(
    string Id,
    string Name,
    SegmentCriteriaDto Criteria,
    int CustomerCount,
    int ReachableCount,
    DateTimeOffset CountedAt,
    string CreatedByStaffId,
    DateTimeOffset CreatedAt)
{
    /// <summary>With the counts taken when the group was saved.</summary>
    public static SegmentDto From(Segment segment) =>
        From(segment, new SegmentCounts(segment.CustomerCount, segment.ReachableCount), segment.CreatedAt);

    public static SegmentDto From(Segment segment, SegmentCounts counts, DateTimeOffset countedAt) =>
        new(
            segment.Id,
            segment.Name,
            SegmentCriteriaDto.From(segment.Criteria),
            counts.Customers,
            counts.Reachable,
            countedAt,
            segment.CreatedByStaffId,
            segment.CreatedAt);
}

public sealed record LoyaltyTierStepDto(string Tier, int MinPoints);

/// <summary>The programme in force. <c>points_expire_after_months</c> (§ 25) is null when points never expire.</summary>
public sealed record LoyaltyRulesDto(
    long SatangPerPoint,
    long PointValueSatang,
    IReadOnlyList<LoyaltyTierStepDto> Tiers,
    int? PointsExpireAfterMonths)
{
    public static LoyaltyRulesDto From(LoyaltySettings settings) =>
        new(
            settings.SatangPerPoint,
            settings.PointValueSatang,
            [.. settings.Tiers.Select(step => new LoyaltyTierStepDto(CrmWire.ToWire(step.Tier), step.MinPoints))],
            settings.PointsExpireAfterMonths);
}

public sealed record NextTierDto(string Tier, int PointsNeeded);

/// <summary>The spendable points that expire soonest, and when (§ 25).</summary>
public sealed record NextExpiryDto(int Points, DateTimeOffset ExpiresAt);

/// <summary>
/// <c>GET</c> and <c>PUT /clinic/settings/loyalty</c> (§ 25). <c>version</c> goes back as
/// <c>If-Match</c>; a clinic that never saved its own reads the defaults at version 0. The four
/// <c>referral_*</c> values are refer a friend (<c>docs/contract-extensions.md § 39</c>).
/// </summary>
public sealed record LoyaltySettingsDto(
    long SatangPerPoint,
    long PointValueSatang,
    int SilverMinPoints,
    int GoldMinPoints,
    int? PointsExpireAfterMonths,
    bool ReferralEnabled,
    int ReferralReferrerPoints,
    int ReferralFriendPoints,
    int ReferralMonthlyCap,
    uint Version,
    DateTimeOffset? UpdatedAt)
{
    public static LoyaltySettingsDto From(LoyaltySettings settings) =>
        new(
            settings.SatangPerPoint,
            settings.PointValueSatang,
            settings.SilverMinPoints,
            settings.GoldMinPoints,
            settings.PointsExpireAfterMonths,
            settings.ReferralEnabled,
            settings.ReferralReferrerPoints,
            settings.ReferralFriendPoints,
            settings.ReferralMonthlyCap,
            settings.Version,
            settings.UpdatedAt);
}

public sealed record LoyaltyEntryDto(
    string Id,
    string Kind,
    int Points,
    string? InvoiceId,
    string? InvoiceNumber,
    string? Note,
    DateTimeOffset OccurredAt,
    DateTimeOffset? ExpiresAt = null,
    string? StaffId = null)
{
    public static LoyaltyEntryDto From(LoyaltyEntry entry, string? invoiceNumber) =>
        new(
            entry.Id,
            CrmWire.ToWire(entry.Kind),
            entry.Points,
            entry.InvoiceId,
            invoiceNumber,
            entry.Note,
            entry.OccurredAt,
            entry.ExpiresAt,
            entry.StaffId);
}

/// <summary><c>POST /staff/loyalty/{customer_id}/adjustments</c> (§ 25): the row written and the balance after.</summary>
public sealed record LoyaltyAdjustmentDto(LoyaltyEntryDto Entry, int PointsBalance, int LifetimePoints);

public sealed record CouponRedemptionDto(
    string Id,
    string CouponId,
    string Code,
    string InvoiceId,
    string? InvoiceNumber,
    long AmountSatang,
    DateTimeOffset RedeemedAt,
    DateTimeOffset? ReleasedAt);

/// <summary><c>GET /staff/loyalty/{customer_id}</c> — "แต้มสะสม ระดับสมาชิก และประวัติแลก".</summary>
public sealed record LoyaltyAccountDto(
    string CustomerId,
    int PointsBalance,
    int LifetimePoints,
    string Tier,
    NextTierDto? NextTier,
    LoyaltyRulesDto Rules,
    int ExpiredPoints,
    NextExpiryDto? NextExpiry,
    IReadOnlyList<LoyaltyEntryDto> History,
    IReadOnlyList<CouponRedemptionDto> CouponRedemptions,
    IReadOnlyList<PackageDto> Packages);
