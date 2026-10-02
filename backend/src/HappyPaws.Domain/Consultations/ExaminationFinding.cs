using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Consultations;

/// <summary>
/// One line of the physical examination — <c>POST /staff/consultations/{id}/examination</c>,
/// "ผลตรวจรายระบบ ปกติ/ผิดปกติ + บันทึก".
/// <para>
/// <see cref="IsNormal"/> is a <c>bool</c>, not a nullable one, and that is the point: a body
/// system that was not examined has no row at all. A tri-state would let "not checked" be recorded
/// as if it were a finding, and in a clinical record the difference between "abdomen normal" and
/// "abdomen not examined" is the whole question.
/// </para>
/// </summary>
public sealed class ExaminationFinding
{
    private ExaminationFinding()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string ConsultationId { get; private set; } = string.Empty;

    /// <summary>The body system examined — <c>cardiovascular</c>, <c>ผิวหนัง</c>, <c>ช่องปาก</c>.</summary>
    public string BodySystem { get; private set; } = string.Empty;

    public bool IsNormal { get; private set; }

    public string? Note { get; private set; }

    public static ExaminationFinding Record(string consultationId, string bodySystem, bool isNormal, string? note)
    {
        if (string.IsNullOrWhiteSpace(bodySystem))
        {
            throw new DomainException("invalid_examination", "ต้องระบุระบบที่ตรวจ");
        }

        if (!isNormal && string.IsNullOrWhiteSpace(note))
        {
            // "Abnormal" on its own is not a finding — it is a flag with no clinical content, and
            // the next vet reading this file cannot act on it.
            throw new DomainException("invalid_examination", "ผลผิดปกติต้องมีบันทึกประกอบ");
        }

        return new ExaminationFinding
        {
            Id = EntityId.New(EntityId.Finding),
            ConsultationId = consultationId,
            BodySystem = bodySystem.Trim(),
            IsNormal = isNormal,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }
}
