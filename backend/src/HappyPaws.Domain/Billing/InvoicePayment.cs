using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Billing;

/// <summary>
/// The tender types the contract names: "เงินสด บัตร PromptPay".
/// <para>
/// Exactly three, and no more. Bank transfer, e-wallet and instalment are all plausible and none of
/// them is in the reference — a fourth member here would be an invented business rule about what
/// this clinic accepts, dressed as an enum.
/// </para>
/// </summary>
public enum PaymentMethod
{
    Cash,
    Card,
    PromptPay,
}

/// <summary>
/// One tender against a bill — <c>POST /staff/invoices/{id}/payments</c>,
/// "แบ่งจ่ายหลายวิธีได้".
/// <para>
/// Split payment is why this is a collection rather than a pair of columns on the invoice. Half in
/// cash and half on a card is one bill and two rows, and each row has to carry its own method and
/// its own reference — the card slip number is what reconciles against the acquirer's statement at
/// the end of the day, and it belongs to the tender, not to the bill.
/// </para>
/// </summary>
public sealed class InvoicePayment
{
    private InvoicePayment()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string InvoiceId { get; private set; } = string.Empty;

    public PaymentMethod Method { get; private set; }

    public long AmountSatang { get; private set; }

    /// <summary>Card approval code, PromptPay transaction id — whatever reconciles this row.</summary>
    public string? Reference { get; private set; }

    public string CollectedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset CollectedAt { get; private set; }

    public Money Amount => new(AmountSatang);

    public static InvoicePayment Create(
        string invoiceId,
        PaymentMethod method,
        long amountSatang,
        string collectedByStaffId,
        DateTimeOffset collectedAt,
        string? reference = null)
    {
        if (amountSatang <= 0)
        {
            // A zero payment records nothing and a negative one is a refund, which is a different
            // endpoint with a different scope and a mandatory reason.
            throw new DomainException("invalid_payment", "ยอดชำระต้องมากกว่าศูนย์");
        }

        return new InvoicePayment
        {
            Id = EntityId.New(EntityId.Payment),
            InvoiceId = invoiceId,
            Method = method,
            AmountSatang = amountSatang,
            CollectedByStaffId = collectedByStaffId,
            CollectedAt = collectedAt,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
        };
    }
}
