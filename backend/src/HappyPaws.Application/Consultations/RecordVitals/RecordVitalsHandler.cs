using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations.RecordVitals;

/// <summary>
/// <c>POST /staff/consultations/{id}/vitals</c> — "ผู้ช่วยกรอกได้ · น้ำหนักเข้ากราฟอัตโนมัติ".
/// </summary>
public sealed record RecordVitalsCommand(
    string ConsultationId,
    decimal? WeightKg = null,
    decimal? TemperatureC = null,
    int? HeartRateBpm = null,
    int? RespiratoryRateBpm = null,
    string? Note = null);

/// <summary>
/// Adds a set of vitals to an open case.
/// <para>
/// The route requires <c>vitals:write</c> rather than <c>consult:write</c>, which is what lets an
/// assistant weigh and take a temperature without being able to write anything else on the case.
/// <c>vet_assistant</c> in the contract's default roles holds exactly that scope and no other
/// clinical write.
/// </para>
/// <para>
/// "น้ำหนักเข้ากราฟอัตโนมัติ" needs no second write: the weight graph reads these rows by patient.
/// A separate weights table would be a copy of the same measurement, free to drift from it.
/// </para>
/// </summary>
public sealed class RecordVitalsHandler(
    ConsultationAccess access,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ConsultationDto> HandleAsync(
        RecordVitalsCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        Consultation? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var consultation = await access.ForStaffAsync(command.ConsultationId, token);

            var reading = consultation.AddVitals(
                staffId,
                clock.UtcNow,
                command.WeightKg,
                command.TemperatureC,
                command.HeartRateBpm,
                command.RespiratoryRateBpm,
                command.Note);

            audit.Record(new AuditEvent(
                AuditActions.VitalsRecorded,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                consultation.BranchId,
                new Dictionary<string, object?>
                {
                    ["vitals_id"] = reading.Id,
                    ["weight_kg"] = reading.WeightKg,
                }));

            updated = consultation;
        }, cancellationToken);

        return ConsultationDto.From(updated!);
    }
}
