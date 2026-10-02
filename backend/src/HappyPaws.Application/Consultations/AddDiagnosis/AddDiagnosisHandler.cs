using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations.AddDiagnosis;

/// <summary>
/// <c>POST /staff/consultations/{id}/diagnoses</c> —
/// "วินิจฉัยหลัก/รอง · ผูกรหัสโรคของคลินิก".
/// </summary>
public sealed record AddDiagnosisCommand(
    string ConsultationId,
    string Description,
    bool IsPrimary = false,
    string? Code = null,
    string? Note = null);

/// <summary>
/// Adds a diagnosis. The aggregate keeps exactly one primary, demoting the previous holder when a
/// new one claims the place — a case with two principal diagnoses cannot be summarised, and a case
/// with none cannot be coded.
/// </summary>
public sealed class AddDiagnosisHandler(
    ConsultationAccess access,
    IAuditTrail audit,
    IUnitOfWork unitOfWork)
{
    public async Task<ConsultationDto> HandleAsync(
        AddDiagnosisCommand command,
        CancellationToken cancellationToken = default)
    {
        Consultation? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var consultation = await access.ForStaffAsync(command.ConsultationId, token);

            var diagnosis = consultation.AddDiagnosis(
                command.Description, command.IsPrimary, command.Code, command.Note);

            audit.Record(new AuditEvent(
                AuditActions.DiagnosisAdded,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                consultation.BranchId,
                new Dictionary<string, object?>
                {
                    ["diagnosis_id"] = diagnosis.Id,
                    ["code"] = diagnosis.Code,
                    ["is_primary"] = diagnosis.IsPrimary,
                }));

            updated = consultation;
        }, cancellationToken);

        return ConsultationDto.From(updated!);
    }
}
