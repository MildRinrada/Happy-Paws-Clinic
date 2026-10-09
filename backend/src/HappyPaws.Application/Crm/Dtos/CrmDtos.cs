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
    string CreatedByStaffId,
    DateTimeOffset CreatedAt)
{
    public static CouponDto From(Coupon coupon) =>
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
            coupon.CreatedByStaffId,
            coupon.CreatedAt);
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
/// A saved customer group. <c>customer_count</c> is who matches now; <c>reachable_count</c> is how
/// many of them agreed to marketing and have a LINE address, which is who a broadcast would reach.
/// </summary>
public sealed record SegmentDto(
    string Id,
    string Name,
    SegmentCriteriaDto Criteria,
    int CustomerCount,
    int ReachableCount,
    string CreatedByStaffId,
    DateTimeOffset CreatedAt)
{
    public static SegmentDto From(Segment segment) =>
        new(
            segment.Id,
            segment.Name,
            SegmentCriteriaDto.From(segment.Criteria),
            segment.CustomerCount,
            segment.ReachableCount,
            segment.CreatedByStaffId,
            segment.CreatedAt);
}

public sealed record LoyaltyTierStepDto(string Tier, int MinPoints);

public sealed record LoyaltyRulesDto(long SatangPerPoint, long PointValueSatang, IReadOnlyList<LoyaltyTierStepDto> Tiers)
{
    public static LoyaltyRulesDto Current { get; } = new(
        LoyaltyRules.SatangPerPoint,
        LoyaltyRules.PointValueSatang,
        [.. LoyaltyRules.Tiers.Select(step => new LoyaltyTierStepDto(CrmWire.ToWire(step.Tier), step.MinPoints))]);
}

public sealed record NextTierDto(string Tier, int PointsNeeded);

public sealed record LoyaltyEntryDto(
    string Id,
    string Kind,
    int Points,
    string? InvoiceId,
    string? InvoiceNumber,
    string? Note,
    DateTimeOffset OccurredAt);

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
    IReadOnlyList<LoyaltyEntryDto> History,
    IReadOnlyList<CouponRedemptionDto> CouponRedemptions,
    IReadOnlyList<PackageDto> Packages);
