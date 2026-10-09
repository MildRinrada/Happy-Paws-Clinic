using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Crm;

/// <summary>
/// One coupon used on one bill: the discount it became, and for whom.
/// <para>
/// A voided bill releases it (the coupon's count goes back down); a refunded one does not, because
/// the discount was given on a sale that happened.
/// </para>
/// </summary>
public sealed class CouponRedemption : Entity
{
    private CouponRedemption()
    {
    }

    public string CouponId { get; private set; } = string.Empty;

    /// <summary>The code as it was at the time, so the history reads without a join.</summary>
    public string Code { get; private set; } = string.Empty;

    public string InvoiceId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>The discount row on the bill.</summary>
    public string DiscountId { get; private set; } = string.Empty;

    public long AmountSatang { get; private set; }

    public string RedeemedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset RedeemedAt { get; private set; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public static CouponRedemption Record(
        Coupon coupon,
        string invoiceId,
        string customerId,
        string discountId,
        long amountSatang,
        string staffId,
        DateTimeOffset now) =>
        new()
        {
            Id = EntityId.New(EntityId.CouponRedemption),
            CouponId = coupon.Id,
            Code = coupon.Code,
            InvoiceId = invoiceId,
            CustomerId = customerId,
            DiscountId = discountId,
            AmountSatang = amountSatang,
            RedeemedByStaffId = staffId,
            RedeemedAt = now,
        };

    public void Release(DateTimeOffset now) => ReleasedAt ??= now;
}
