using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Prescriptions;

/// <summary>
/// A drug written on a case — <c>POST /staff/consultations/{id}/prescriptions</c>,
/// "ตรวจแพ้ยาและตัดสต๊อกเมื่อจ่ายจริง".
/// <para>
/// Lives in its own module rather than under Consultations because the contract queries it
/// independently: <c>GET /staff/prescriptions?pet_id=</c> — "ยาที่เคยจ่ายทั้งหมด · ตรวจซ้ำซ้อน" —
/// asks what a patient has ever been given, across every visit. <see cref="PetId"/> is carried
/// here so that question is one index lookup rather than a walk through consultations.
/// </para>
/// <para>
/// Writing a prescription and dispensing it are separate events, which is why
/// <see cref="DispensedAt"/> is nullable and stock is untouched until <c>rx:dispense</c>. A drug
/// written but never collected must not decrement inventory, and is not charged for either.
/// </para>
/// <para>
/// What the dispense did is kept here: which inventory item the drug was matched to, whether stock
/// was actually taken, and the unit price charged. The price is captured at the moment of handing
/// over, like a treatment's, so a later price change does not restate it; a dispense before the
/// case closes is billed from this field when the case's invoice is raised.
/// </para>
/// </summary>
public sealed class Prescription : Entity
{
    private Prescription()
    {
    }

    public string ConsultationId { get; private set; } = string.Empty;

    public string PetId { get; private set; } = string.Empty;

    public string PrescribedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset PrescribedAt { get; private set; }

    public string DrugName { get; private set; } = string.Empty;

    /// <summary>Amount per administration, as written — <c>1 เม็ด</c>, <c>0.5 ml</c>.</summary>
    public string Dose { get; private set; } = string.Empty;

    /// <summary>How often — <c>วันละ 2 ครั้ง หลังอาหาร</c>.</summary>
    public string Frequency { get; private set; } = string.Empty;

    public int DurationDays { get; private set; }

    /// <summary>Total units to hand over, for the label and for the stock movement.</summary>
    public int Quantity { get; private set; }

    public string? Instructions { get; private set; }

    /// <summary>
    /// Set when the vet wrote this drug over a recorded allergy and acknowledged it. Never a
    /// silent override — the acknowledgement is part of the record and is audited.
    /// </summary>
    public string? AllergyAcknowledgement { get; private set; }

    public DateTimeOffset? DispensedAt { get; private set; }

    public string? DispensedByStaffId { get; private set; }

    /// <summary>The inventory item this drug was matched to at dispense. Null when nothing matched.</summary>
    public string? InventoryItemId { get; private set; }

    /// <summary>True when the dispense took the quantity off the branch's stock.</summary>
    public bool StockDeducted { get; private set; }

    /// <summary>
    /// The unit price charged for it, captured at dispense. Null when nothing was charged: the drug
    /// matched no item, or the item is priced at zero.
    /// </summary>
    public long? ChargedUnitPriceSatang { get; private set; }

    public bool IsDispensed => DispensedAt is not null;

    /// <summary>What the dispense charged in total, or zero.</summary>
    public long ChargedSatang => (ChargedUnitPriceSatang ?? 0) * Quantity;

    /// <summary>When the course runs out, counting from when it was written.</summary>
    public DateTimeOffset CourseEndsAt => PrescribedAt.AddDays(DurationDays);

    /// <summary>True while the patient should still be taking it.</summary>
    public bool IsActiveAt(DateTimeOffset now) => now >= PrescribedAt && now < CourseEndsAt;

    /// <summary>
    /// The drug without its strength or form: "Amoxicillin 250mg" and "amoxicillin 500 mg" are both
    /// <c>amoxicillin</c>.
    /// <para>
    /// The first word, lower-cased. As blunt as the allergy check and for the same reason: there is
    /// no drug database here. Two brand names for one molecule are not recognised as the same drug.
    /// </para>
    /// </summary>
    public string DrugKey => KeyOf(DrugName);

    /// <summary>
    /// "ตรวจซ้ำซ้อน" — whether the other prescription is the same drug over an overlapping course,
    /// which is a double dose unless someone meant it.
    /// </summary>
    public bool DuplicatesTherapyOf(Prescription other) =>
        other.Id != Id
        && other.DrugKey == DrugKey
        && PrescribedAt < other.CourseEndsAt
        && other.PrescribedAt < CourseEndsAt;

    public static string KeyOf(string drugName) =>
        drugName.Trim().Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries) is [var first, ..]
            ? first.ToLowerInvariant()
            : string.Empty;

    public static Prescription Write(
        string consultationId,
        string petId,
        string prescribedByStaffId,
        DateTimeOffset prescribedAt,
        string drugName,
        string dose,
        string frequency,
        int durationDays,
        int quantity,
        string? instructions = null,
        string? allergyAcknowledgement = null)
    {
        if (string.IsNullOrWhiteSpace(drugName))
        {
            throw new DomainException("invalid_prescription", "ต้องระบุชื่อยา");
        }

        if (string.IsNullOrWhiteSpace(dose) || string.IsNullOrWhiteSpace(frequency))
        {
            throw new DomainException("invalid_prescription", "ต้องระบุขนาดยาและความถี่");
        }

        if (durationDays <= 0 || quantity <= 0)
        {
            throw new DomainException("invalid_prescription", "จำนวนวันและจำนวนยาต้องมากกว่าศูนย์");
        }

        return new Prescription
        {
            Id = EntityId.New(EntityId.Prescription),
            ConsultationId = consultationId,
            PetId = petId,
            PrescribedByStaffId = prescribedByStaffId,
            PrescribedAt = prescribedAt,
            DrugName = drugName.Trim(),
            Dose = dose.Trim(),
            Frequency = frequency.Trim(),
            DurationDays = durationDays,
            Quantity = quantity,
            Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim(),
            AllergyAcknowledgement = string.IsNullOrWhiteSpace(allergyAcknowledgement)
                ? null
                : allergyAcknowledgement.Trim(),
        };
    }

    /// <summary><c>POST /staff/prescriptions/{id}/dispense</c> — "จ่ายยาจริง · ตัดสต๊อกและพิมพ์ฉลาก".</summary>
    public void Dispense(string byStaffId, DateTimeOffset at)
    {
        if (IsDispensed)
        {
            throw new StateConflictException(nameof(Prescription), "dispensed", nameof(Dispense));
        }

        DispensedAt = at;
        DispensedByStaffId = byStaffId;
    }

    /// <summary>
    /// Records what the dispense did to stock and what it charged. Only on a prescription that has
    /// just been dispensed, and only once.
    /// </summary>
    public void RecordDispenseOutcome(string? inventoryItemId, bool stockDeducted, long? chargedUnitPriceSatang)
    {
        if (!IsDispensed)
        {
            throw new StateConflictException(nameof(Prescription), "written", nameof(RecordDispenseOutcome));
        }

        if (stockDeducted && inventoryItemId is null)
        {
            throw new DomainException("invalid_prescription", "ตัดสต๊อกได้เฉพาะยาที่ผูกกับรายการในคลัง");
        }

        if (chargedUnitPriceSatang is <= 0)
        {
            chargedUnitPriceSatang = null;
        }

        InventoryItemId = inventoryItemId;
        StockDeducted = stockDeducted;
        ChargedUnitPriceSatang = inventoryItemId is null ? null : chargedUnitPriceSatang;
    }
}
