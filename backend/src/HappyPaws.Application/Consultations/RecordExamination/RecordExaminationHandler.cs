using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations.RecordExamination;

/// <summary>
/// <c>POST /staff/consultations/{id}/examination</c> —
/// "ผลตรวจรายระบบ ปกติ/ผิดปกติ + บันทึก".
/// </summary>
/// <param name="ConsultationId">The open case.</param>
/// <param name="Findings">The whole form. A body system omitted here was not examined.</param>
public sealed record RecordExaminationCommand(
    string ConsultationId,
    IReadOnlyList<ExaminationEntry> Findings);

/// <summary>One line of the examination form.</summary>
public sealed record ExaminationEntry(string BodySystem, bool IsNormal, string? Note = null);

/// <summary>
/// Records the physical examination.
/// <para>
/// A second submission <b>replaces</b> the first. The contract describes one form covering every
/// body system, so re-posting is the corrected form rather than a second examination — and
/// appending would leave two contradictory answers for the same system with no way to tell which
/// one the vet meant.
/// </para>
/// </summary>
public sealed class RecordExaminationHandler(
    ConsultationAccess access,
    IClinicSettingsStore settings,
    IAuditTrail audit,
    IUnitOfWork unitOfWork)
{
    public async Task<ConsultationDto> HandleAsync(
        RecordExaminationCommand command,
        CancellationToken cancellationToken = default)
    {
        Consultation? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var consultation = await access.ForStaffAsync(command.ConsultationId, token);

            await EnsureOnTheFormAsync(command.Findings, token);

            consultation.ReplaceExamination(
                command.Findings.Select(entry => (entry.BodySystem, entry.IsNormal, entry.Note)));

            audit.Record(new AuditEvent(
                AuditActions.ExaminationRecorded,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                consultation.BranchId,
                new Dictionary<string, object?>
                {
                    ["systems"] = consultation.Findings.Count,
                    ["abnormal"] = consultation.Findings.Count(finding => !finding.IsNormal),
                }));

            updated = consultation;
        }, cancellationToken);

        return ConsultationDto.From(updated!);
    }

    /// <summary>
    /// Holds the findings to the clinic's examination form — the "แบบฟอร์มตรวจ" of
    /// <c>PATCH /clinic/settings/medical-record</c>. A clinic that has configured no form accepts
    /// any body system, which is how this endpoint behaved before the setting existed.
    /// </summary>
    private async Task EnsureOnTheFormAsync(
        IReadOnlyList<ExaminationEntry> findings,
        CancellationToken cancellationToken)
    {
        var form = await settings.MedicalRecordAsync(cancellationToken);

        var offForm = findings
            .Select(entry => entry.BodySystem)
            .Where(system => !string.IsNullOrWhiteSpace(system) && !form.AllowsExaminationSystem(system))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (offForm.Length > 0)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["findings"] = [$"ระบบที่ไม่อยู่ในแบบฟอร์มตรวจของคลินิก: {string.Join(", ", offForm)}"],
            });
        }
    }
}
