using HappyPaws.Application.Crm.CreateCoupon;
using HappyPaws.Application.Crm.CreatePackage;
using HappyPaws.Application.Crm.CreateSegment;

namespace HappyPaws.Api.Contracts.Crm;

/// <summary>
/// <c>POST /staff/packages</c>:
/// <code>
/// { "customer_id": "cus_...", "pet_id": "pet_...", "name": "ตรวจสุขภาพ 4 ครั้ง",
///   "service_ids": ["svc_..."], "total_uses": 4, "price_satang": 180000,
///   "expires_on": "2027-10-09", "branch_id": "brn_..." }
/// </code>
/// </summary>
public sealed record CreatePackageRequest(
    string? CustomerId = null,
    string? Name = null,
    IReadOnlyList<string>? ServiceIds = null,
    int? TotalUses = null,
    long? PriceSatang = null,
    DateOnly? ExpiresOn = null,
    string? PetId = null,
    string? BranchId = null,
    string? Note = null)
{
    public CreatePackageCommand ToCommand() =>
        new(
            CustomerId ?? string.Empty,
            Name ?? string.Empty,
            ServiceIds ?? [],
            TotalUses ?? 0,
            PriceSatang ?? -1,
            ExpiresOn,
            PetId,
            BranchId,
            Note);
}

/// <summary>
/// <c>POST /staff/coupons</c>:
/// <code>
/// { "code": "VACCINE10", "name": "ลดค่าวัคซีน 10%", "percent": 10, "service_ids": ["svc_..."],
///   "max_redemptions": 100, "per_customer_limit": 1, "valid_from": "2026-10-01", "valid_until": "2026-12-31" }
/// </code>
/// </summary>
public sealed record CreateCouponRequest(
    string? Name = null,
    string? Code = null,
    decimal? Percent = null,
    long? AmountSatang = null,
    IReadOnlyList<string>? ServiceIds = null,
    int? MaxRedemptions = null,
    int? PerCustomerLimit = null,
    DateOnly? ValidFrom = null,
    DateOnly? ValidUntil = null,
    string? BranchId = null)
{
    public CreateCouponCommand ToCommand() =>
        new(
            Name ?? string.Empty,
            Code,
            Percent,
            AmountSatang,
            ServiceIds,
            MaxRedemptions,
            PerCustomerLimit,
            ValidFrom,
            ValidUntil,
            BranchId);
}

/// <summary>
/// <c>POST /staff/segments</c>:
/// <code>
/// { "name": "สุนัขที่ไม่มา 6 เดือน", "criteria": { "species": ["สุนัข"], "inactive_days": 180 } }
/// </code>
/// </summary>
public sealed record CreateSegmentRequest(string? Name = null, SegmentCriteriaRequest? Criteria = null)
{
    public CreateSegmentCommand ToCommand() => new(Name ?? string.Empty, Criteria?.ToInput());
}

public sealed record SegmentCriteriaRequest(
    IReadOnlyList<string>? Species = null,
    int? InactiveDays = null,
    int? VisitedWithinDays = null,
    long? MinSpendSatang = null,
    IReadOnlyList<string>? Tiers = null,
    bool? HasActivePackage = null,
    string? BranchId = null)
{
    public SegmentCriteriaInput ToInput() =>
        new(Species, InactiveDays, VisitedWithinDays, MinSpendSatang, Tiers, HasActivePackage, BranchId);
}

/// <summary>
/// <c>POST /staff/coupons/{id}/deactivate</c> (<c>docs/contract-extensions.md § 25</c>):
/// <code>{ "reason": "โค้ดหลุดไปในกลุ่มแชต" }</code>
/// </summary>
public sealed record DeactivateCouponRequest(string? Reason = null);

/// <summary>
/// <c>POST /staff/loyalty/{customer_id}/adjustments</c> (§ 25):
/// <code>{ "points": 50, "reason": "ชดเชยนัดที่คลินิกเลื่อน" }</code>
/// </summary>
public sealed record AdjustPointsRequest(int? Points = null, string? Reason = null);

/// <summary>
/// <c>PUT /clinic/settings/loyalty</c> (§ 25):
/// <code>{ "satang_per_point": 10000, "point_value_satang": 100, "silver_min_points": 200,
///   "gold_min_points": 1000, "points_expire_after_months": 12 }</code>
/// The <c>referral_*</c> values (§ 39) are optional; one not sent keeps its current value.
/// </summary>
public sealed record ReplaceLoyaltySettingsRequest(
    long? SatangPerPoint = null,
    long? PointValueSatang = null,
    int? SilverMinPoints = null,
    int? GoldMinPoints = null,
    int? PointsExpireAfterMonths = null,
    bool? ReferralEnabled = null,
    int? ReferralReferrerPoints = null,
    int? ReferralFriendPoints = null,
    int? ReferralMonthlyCap = null);
