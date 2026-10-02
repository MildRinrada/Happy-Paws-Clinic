using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Patients;

/// <summary>How badly a patient reacts. Drives whether a prescription is merely flagged or refused.</summary>
public enum AllergySeverity
{
    Mild,
    Moderate,
    Severe,
}

/// <summary>
/// A recorded allergy — <c>POST /staff/pets/{id}/allergies</c>,
/// "แสดงเป็นแถบเตือนแดงทุกหน้าที่เกี่ยวข้อง".
/// <para>
/// Built here, ahead of the rest of the Tier-2 patient module, because the contract makes the
/// prescription endpoint responsible for "ตรวจแพ้ยา" — checking allergies. A check with nothing to
/// check against is decoration, and this is the table that makes it real.
/// </para>
/// <para>
/// Allergies are not deleted. A record entered in error is <see cref="Withdraw"/>n with a reason,
/// because "this patient was believed allergic to X for two years" is itself clinically relevant.
/// </para>
/// </summary>
public sealed class PetAllergy : Entity
{
    private PetAllergy()
    {
    }

    public string PetId { get; private set; } = string.Empty;

    /// <summary>The drug or substance, as the clinic writes it — <c>Penicillin</c>, <c>ไก่</c>.</summary>
    public string Substance { get; private set; } = string.Empty;

    public AllergySeverity Severity { get; private set; }

    /// <summary>What happened — "ผื่นแดง หายใจลำบาก".</summary>
    public string? Reaction { get; private set; }

    public string RecordedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; private set; }

    public DateTimeOffset? WithdrawnAt { get; private set; }

    public string? WithdrawnReason { get; private set; }

    public bool IsActive => WithdrawnAt is null;

    public static PetAllergy Record(
        string petId,
        string substance,
        AllergySeverity severity,
        string recordedByStaffId,
        DateTimeOffset recordedAt,
        string? reaction = null)
    {
        if (string.IsNullOrWhiteSpace(substance))
        {
            throw new DomainException("invalid_allergy", "ต้องระบุสารที่แพ้");
        }

        return new PetAllergy
        {
            Id = EntityId.New(EntityId.Allergy),
            PetId = petId,
            Substance = substance.Trim(),
            Severity = severity,
            Reaction = string.IsNullOrWhiteSpace(reaction) ? null : reaction.Trim(),
            RecordedByStaffId = recordedByStaffId,
            RecordedAt = recordedAt,
        };
    }

    public void Withdraw(DateTimeOffset at, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("invalid_allergy", "ต้องระบุเหตุผลในการถอนรายการแพ้ยา");
        }

        WithdrawnAt ??= at;
        WithdrawnReason ??= reason.Trim();
    }

    /// <summary>
    /// Whether a drug name plausibly names this allergen.
    /// <para>
    /// A substring match in both directions, case-insensitively: "Amoxicillin 250mg" matches an
    /// allergy recorded as "Amoxicillin", and an allergy recorded as "Amoxicillin 250mg" matches a
    /// prescription for "Amoxicillin". It does <b>not</b> know that amoxicillin is a penicillin —
    /// drug-class knowledge needs a drug database, which this system does not have. The check is
    /// therefore a safety net, not a guarantee, and it is documented as such where it is used.
    /// </para>
    /// </summary>
    public bool Matches(string drugName) =>
        !string.IsNullOrWhiteSpace(drugName)
        && (drugName.Contains(Substance, StringComparison.OrdinalIgnoreCase)
            || Substance.Contains(drugName.Trim(), StringComparison.OrdinalIgnoreCase));
}
