using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.MedicalRecords;

/// <summary>
/// One correction to a signed record, kept forever alongside every other.
/// <para>
/// Two payloads, deliberately. <see cref="ContentJson"/> is the whole document as it reads after
/// this change, so any version can be rendered without replaying a chain of patches.
/// <see cref="ChangesJson"/> is the field-level diff — "ใครแก้อะไรเมื่อไหร่ · diff รายฟิลด์" — which
/// is what <c>GET /staff/medical-records/{id}/audit</c> shows and what a reviewer actually reads.
/// Storing only the diff would make rendering fragile; storing only the content would make the
/// question "what changed?" a text comparison performed at read time.
/// </para>
/// </summary>
public sealed class MedicalRecordAmendment
{
    private MedicalRecordAmendment()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string MedicalRecordId { get; private set; } = string.Empty;

    /// <summary>The full document as it reads after this amendment.</summary>
    public string ContentJson { get; private set; } = string.Empty;

    /// <summary>Field-level diff against the version this amendment replaced.</summary>
    public string ChangesJson { get; private set; } = string.Empty;

    /// <summary>Why the correction was made. Required.</summary>
    public string Reason { get; private set; } = string.Empty;

    public string ByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    internal static MedicalRecordAmendment Create(
        string medicalRecordId,
        string contentJson,
        string changesJson,
        string reason,
        string byStaffId,
        DateTimeOffset at) =>
        new()
        {
            Id = EntityId.New(EntityId.Amendment),
            MedicalRecordId = medicalRecordId,
            ContentJson = contentJson,
            ChangesJson = changesJson,
            Reason = reason.Trim(),
            ByStaffId = byStaffId,
            CreatedAt = at,
        };
}
