using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Consultations;

/// <summary>
/// A diagnosis on a case — <c>POST /staff/consultations/{id}/diagnoses</c>,
/// "วินิจฉัยหลัก/รอง · ผูกรหัสโรคของคลินิก".
/// <para>
/// <see cref="Code"/> is the clinic's own disease code and is optional: the contract says codes are
/// the clinic's ("รหัสโรคของคลินิก"), which means a clinic that has not built its list yet must
/// still be able to record a diagnosis in words.
/// </para>
/// </summary>
public sealed class Diagnosis
{
    private Diagnosis()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string ConsultationId { get; private set; } = string.Empty;

    /// <summary>The clinic's disease code, when the clinic keeps one.</summary>
    public string? Code { get; private set; }

    public string Description { get; private set; } = string.Empty;

    /// <summary>Exactly one diagnosis on a case is primary; the aggregate enforces it.</summary>
    public bool IsPrimary { get; private set; }

    public string? Note { get; private set; }

    public static Diagnosis Record(string consultationId, string description, bool isPrimary, string? code, string? note)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("invalid_diagnosis", "ต้องระบุคำวินิจฉัย");
        }

        return new Diagnosis
        {
            Id = EntityId.New(EntityId.Diagnosis),
            ConsultationId = consultationId,
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            Description = description.Trim(),
            IsPrimary = isPrimary,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }

    /// <summary>Called by the aggregate when a newer diagnosis takes over as primary.</summary>
    internal void Demote() => IsPrimary = false;
}
