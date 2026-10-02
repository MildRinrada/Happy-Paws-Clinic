using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Billing;

/// <summary>
/// The document handed to the customer — <c>POST /staff/invoices/{id}/receipt</c>,
/// "ออกใบเสร็จ/ใบกำกับภาษี · ส่งเข้า LINE".
/// <para>
/// One per invoice, and that is a rule rather than a simplification. A second original of a
/// ใบกำกับภาษี is a tax problem, not a convenience — Thai practice is that a reissue is marked as a
/// copy of the first, and a system that quietly minted a second number would put the clinic's books
/// out of agreement with the revenue department's. Asking again returns the document already
/// issued.
/// </para>
/// <para>
/// <see cref="AmountSatang"/> is what had been received when the document was issued, captured
/// here rather than read off the invoice. A receipt states a sum that was handed over at a moment;
/// recomputing it later would make the printed copy and the stored one disagree the first time
/// anything else moved.
/// </para>
/// </summary>
public sealed class Receipt
{
    private Receipt()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string InvoiceId { get; private set; } = string.Empty;

    /// <summary>The running document number. Unique across the clinic.</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>True for a ใบกำกับภาษี, which carries the buyer's tax registration.</summary>
    public bool IsTaxInvoice { get; private set; }

    public long AmountSatang { get; private set; }

    /// <summary>Buyer's tax id. Required on a tax invoice, absent on a plain receipt.</summary>
    public string? TaxId { get; private set; }

    /// <summary>The name the document is made out to, when it differs from the customer record.</summary>
    public string? LegalName { get; private set; }

    public string? BillingAddress { get; private set; }

    public string IssuedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; private set; }

    public Money Amount => new(AmountSatang);

    public static Receipt Issue(
        string invoiceId,
        string number,
        long amountSatang,
        string issuedByStaffId,
        DateTimeOffset issuedAt,
        bool isTaxInvoice = false,
        string? taxId = null,
        string? legalName = null,
        string? billingAddress = null)
    {
        if (isTaxInvoice && string.IsNullOrWhiteSpace(taxId))
        {
            // Without it the document is not a tax invoice, whatever it is titled — and the
            // customer discovers that when their accountant refuses it, months later.
            throw new DomainException("invalid_receipt", "ใบกำกับภาษีต้องระบุเลขประจำตัวผู้เสียภาษี");
        }

        return new Receipt
        {
            Id = EntityId.New(EntityId.Receipt),
            InvoiceId = invoiceId,
            Number = number,
            IsTaxInvoice = isTaxInvoice,
            AmountSatang = amountSatang,
            TaxId = Trimmed(taxId),
            LegalName = Trimmed(legalName),
            BillingAddress = Trimmed(billingAddress),
            IssuedByStaffId = issuedByStaffId,
            IssuedAt = issuedAt,
        };
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
