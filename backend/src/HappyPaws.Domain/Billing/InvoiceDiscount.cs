using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Billing;

/// <summary>
/// A reduction applied to a bill — <c>POST /staff/invoices/{id}/discounts</c>,
/// "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ".
/// <para>
/// Both of those words are enforced here rather than left to the caller. A discount is the one
/// operation in this module that moves money out of the clinic on somebody's say-so, and a row that
/// cannot name who said so is indistinguishable from theft when the month is reconciled.
/// </para>
/// <para>
/// A percentage discount is resolved to satang at the moment it is applied and the resolved amount
/// is what is stored. <see cref="Percent"/> is kept alongside it so the receipt can print "10%",
/// but it is never recomputed: a bill that later gained a line must not silently enlarge a discount
/// somebody already approved.
/// </para>
/// </summary>
public sealed class InvoiceDiscount
{
    private InvoiceDiscount()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string InvoiceId { get; private set; } = string.Empty;

    public long AmountSatang { get; private set; }

    /// <summary>The rate this was asked for as, when it was asked for as a rate. Display only.</summary>
    public decimal? Percent { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    /// <summary>Who authorised it — "ผู้อนุมัติ".</summary>
    public string ApprovedByStaffId { get; private set; } = string.Empty;

    /// <summary>Who keyed it in. Usually, but not always, the same person.</summary>
    public string AppliedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset AppliedAt { get; private set; }

    public Money Amount => new(AmountSatang);

    public static InvoiceDiscount Create(
        string invoiceId,
        long amountSatang,
        string reason,
        string approvedByStaffId,
        string appliedByStaffId,
        DateTimeOffset appliedAt,
        decimal? percent = null)
    {
        if (amountSatang <= 0)
        {
            throw new DomainException("invalid_discount", "ส่วนลดต้องมากกว่าศูนย์");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("invalid_discount", "ต้องระบุเหตุผลของส่วนลด");
        }

        if (string.IsNullOrWhiteSpace(approvedByStaffId))
        {
            throw new DomainException("invalid_discount", "ต้องระบุผู้อนุมัติส่วนลด");
        }

        return new InvoiceDiscount
        {
            Id = EntityId.New(EntityId.Discount),
            InvoiceId = invoiceId,
            AmountSatang = amountSatang,
            Percent = percent,
            Reason = reason.Trim(),
            ApprovedByStaffId = approvedByStaffId,
            AppliedByStaffId = appliedByStaffId,
            AppliedAt = appliedAt,
        };
    }

    /// <summary>
    /// Resolves a percentage against a base amount, rounding <b>down</b> to the satang.
    /// <para>
    /// Down, not to nearest: rounding a discount up gives away money the approver did not approve,
    /// and the largest error this can make is one satang in the clinic's favour on a rate somebody
    /// chose as a round number anyway.
    /// </para>
    /// </summary>
    public static long ResolvePercent(decimal percent, long baseSatang)
    {
        if (percent is <= 0 or > 100)
        {
            throw new DomainException("invalid_discount", "เปอร์เซ็นต์ส่วนลดต้องอยู่ระหว่าง 0 ถึง 100");
        }

        return (long)Math.Floor(baseSatang * percent / 100m);
    }
}
