using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Consultations;

/// <summary>
/// A procedure performed during the visit — <c>POST /staff/consultations/{id}/treatments</c>,
/// "หัตถการที่ทำในคลินิก · เข้าบิลอัตโนมัติ".
/// <para>
/// The row carries its own price rather than pointing at the service catalogue for one. A price
/// list changes; a bill raised last March must still say what was charged last March, and a
/// treatment resolved through today's catalogue would quietly restate history every time the
/// clinic adjusts a fee.
/// </para>
/// <para>
/// "เข้าบิลอัตโนมัติ" is why <see cref="LineTotal"/> exists here rather than in the Billing module:
/// closing the case turns these rows into invoice lines, and the arithmetic that produces the
/// amount belongs with the fact, not with the document that later prints it.
/// </para>
/// </summary>
public sealed class Treatment
{
    private Treatment()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string ConsultationId { get; private set; } = string.Empty;

    /// <summary>The catalogue entry this came from, when it came from one.</summary>
    public string? ServiceId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    /// <summary>Unit price in satang, captured at the moment of treatment.</summary>
    public long UnitPriceSatang { get; private set; }

    public string? Note { get; private set; }

    public string PerformedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset PerformedAt { get; private set; }

    public Money LineTotal => new(UnitPriceSatang * Quantity);

    public static Treatment Record(
        string consultationId,
        string name,
        int quantity,
        long unitPriceSatang,
        string performedByStaffId,
        DateTimeOffset performedAt,
        string? serviceId = null,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("invalid_treatment", "ต้องระบุชื่อหัตถการ");
        }

        if (quantity <= 0)
        {
            throw new DomainException("invalid_treatment", "จำนวนต้องมากกว่าศูนย์");
        }

        if (unitPriceSatang < 0)
        {
            // Zero is legitimate — a courtesy re-check, a procedure included in a package. A
            // negative is a discount, and discounts belong on the invoice where they are
            // authorised by `invoice:discount`, not smuggled in as a treatment line.
            throw new DomainException("invalid_treatment", "ราคาต่อหน่วยติดลบไม่ได้");
        }

        return new Treatment
        {
            Id = EntityId.New(EntityId.Treatment),
            ConsultationId = consultationId,
            ServiceId = string.IsNullOrWhiteSpace(serviceId) ? null : serviceId,
            Name = name.Trim(),
            Quantity = quantity,
            UnitPriceSatang = unitPriceSatang,
            PerformedByStaffId = performedByStaffId,
            PerformedAt = performedAt,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }
}
