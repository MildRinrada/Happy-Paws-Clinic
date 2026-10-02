using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Billing;

/// <summary>
/// Money given back — <c>POST /staff/invoices/{id}/refunds</c>,
/// "คืนเงินบางส่วนหรือทั้งใบ · ผูกเหตุผล".
/// <para>
/// The reason is required, and "ผูกเหตุผล" is the whole point of the row. A refund is the only
/// operation here that reverses a completed transaction, and six months later the difference
/// between a goodwill gesture, a billing error and a disputed charge is nowhere except in this
/// field.
/// </para>
/// <para>
/// <see cref="PaymentId"/> is optional because a refund does not always map onto one tender: a bill
/// settled half in cash and half by card, refunded in full, is one reversal against two payments.
/// When the counter can say which tender is being reversed, it should, and the acquirer usually
/// requires it for a card.
/// </para>
/// </summary>
public sealed class InvoiceRefund
{
    private InvoiceRefund()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string InvoiceId { get; private set; } = string.Empty;

    /// <summary>The tender being reversed, when the refund maps onto exactly one.</summary>
    public string? PaymentId { get; private set; }

    public PaymentMethod Method { get; private set; }

    public long AmountSatang { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string RefundedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset RefundedAt { get; private set; }

    public Money Amount => new(AmountSatang);

    public static InvoiceRefund Create(
        string invoiceId,
        PaymentMethod method,
        long amountSatang,
        string reason,
        string refundedByStaffId,
        DateTimeOffset refundedAt,
        string? paymentId = null)
    {
        if (amountSatang <= 0)
        {
            throw new DomainException("invalid_refund", "ยอดคืนเงินต้องมากกว่าศูนย์");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("invalid_refund", "ต้องระบุเหตุผลในการคืนเงิน");
        }

        return new InvoiceRefund
        {
            Id = EntityId.New(EntityId.Refund),
            InvoiceId = invoiceId,
            PaymentId = string.IsNullOrWhiteSpace(paymentId) ? null : paymentId,
            Method = method,
            AmountSatang = amountSatang,
            Reason = reason.Trim(),
            RefundedByStaffId = refundedByStaffId,
            RefundedAt = refundedAt,
        };
    }
}
