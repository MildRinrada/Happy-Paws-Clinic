using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Billing;

/// <summary>
/// Where a billed line came from.
/// <para>
/// Not decoration: <c>GET /staff/analytics/services</c> asks for "กำไรต่อบริการหลังหักยา" — profit
/// per service after deducting medicine — which cannot be answered by a bill whose lines are all
/// the same kind of thing. The split has to exist at the moment the line is written, because
/// nothing later can tell a ฿450 consultation fee from ฿450 of amoxicillin.
/// </para>
/// </summary>
public enum InvoiceLineKind
{
    /// <summary>A catalogue service — the consultation fee, a vaccination, a grooming slot.</summary>
    Service,

    /// <summary>A procedure performed in the room — "หัตถการที่ทำในคลินิก".</summary>
    Treatment,

    /// <summary>Medicine dispensed. The deduction in "กำไรต่อบริการหลังหักยา".</summary>
    Medicine,

    /// <summary>Anything the counter typed by hand: a consumable, a boarding night, a late fee.</summary>
    Other,
}

/// <summary>
/// One billed row — a description, a quantity and the price that was actually charged.
/// <para>
/// The price is captured here rather than resolved through the service catalogue, for the same
/// reason <c>Treatment</c> captures its own: a bill raised last March must still say what was
/// charged last March. A line that resolved today's price would quietly restate every historical
/// invoice each time the clinic adjusts a fee — and the one document a clinic must be able to
/// reprint unchanged is the one it gave a customer.
/// </para>
/// </summary>
public sealed class InvoiceLine
{
    private InvoiceLine()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string InvoiceId { get; private set; } = string.Empty;

    public InvoiceLineKind Kind { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public long UnitPriceSatang { get; private set; }

    /// <summary>The treatment, prescription or service row this line was raised from, when there was one.</summary>
    public string? SourceId { get; private set; }

    public Money LineTotal => new(UnitPriceSatang * Quantity);

    public static InvoiceLine Create(
        string invoiceId,
        InvoiceLineKind kind,
        string description,
        int quantity,
        long unitPriceSatang,
        string? sourceId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("invalid_invoice_line", "ต้องระบุรายละเอียดของรายการ");
        }

        if (quantity <= 0)
        {
            throw new DomainException("invalid_invoice_line", "จำนวนต้องมากกว่าศูนย์");
        }

        if (unitPriceSatang < 0)
        {
            // A negative line would be a discount wearing a disguise, and discounts are gated by
            // `invoice:discount` and require an approver. Letting one in as a line item would put
            // the clinic's whole discount policy behind `invoice:write`.
            throw new DomainException("invalid_invoice_line", "ราคาต่อหน่วยติดลบไม่ได้");
        }

        return new InvoiceLine
        {
            Id = EntityId.New(EntityId.InvoiceLine),
            InvoiceId = invoiceId,
            Kind = kind,
            Description = description.Trim(),
            Quantity = quantity,
            UnitPriceSatang = unitPriceSatang,
            SourceId = string.IsNullOrWhiteSpace(sourceId) ? null : sourceId,
        };
    }
}
