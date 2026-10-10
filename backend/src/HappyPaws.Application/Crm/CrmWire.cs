using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm;

/// <summary>The snake_case names CRM values travel under.</summary>
public static class CrmWire
{
    public static string ToWire(PackageStatus status) => status switch
    {
        PackageStatus.PendingPayment => "pending_payment",
        PackageStatus.Active => "active",
        PackageStatus.UsedUp => "used_up",
        PackageStatus.Expired => "expired",
        PackageStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static string ToWire(LoyaltyTier tier) => tier switch
    {
        LoyaltyTier.Member => "member",
        LoyaltyTier.Silver => "silver",
        LoyaltyTier.Gold => "gold",
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    public static string ToWire(LoyaltyEntryKind kind) => kind switch
    {
        LoyaltyEntryKind.Earn => "earn",
        LoyaltyEntryKind.Reverse => "reverse",
        LoyaltyEntryKind.Redeem => "redeem",
        LoyaltyEntryKind.Restore => "restore",
        LoyaltyEntryKind.Adjust => "adjust",
        LoyaltyEntryKind.Referral => "referral",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static string ToWire(CouponStatus status) => status switch
    {
        CouponStatus.Active => "active",
        CouponStatus.Scheduled => "scheduled",
        CouponStatus.UsedUp => "used_up",
        CouponStatus.Expired => "expired",
        CouponStatus.Deactivated => "deactivated",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static CouponStatus? ParseCouponStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "active" => CouponStatus.Active,
        "scheduled" => CouponStatus.Scheduled,
        "used_up" => CouponStatus.UsedUp,
        "expired" => CouponStatus.Expired,
        "deactivated" => CouponStatus.Deactivated,
        _ => null,
    };

    public static PackageStatus? ParsePackageStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "pending_payment" => PackageStatus.PendingPayment,
        "active" => PackageStatus.Active,
        "used_up" => PackageStatus.UsedUp,
        "expired" => PackageStatus.Expired,
        "cancelled" => PackageStatus.Cancelled,
        _ => null,
    };

    public static LoyaltyTier? ParseTier(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "member" => LoyaltyTier.Member,
        "silver" => LoyaltyTier.Silver,
        "gold" => LoyaltyTier.Gold,
        _ => null,
    };
}
