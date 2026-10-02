using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations.AddTreatment;

/// <summary>
/// <c>POST /staff/consultations/{id}/treatments</c> —
/// "หัตถการที่ทำในคลินิก · เข้าบิลอัตโนมัติ".
/// </summary>
public sealed record AddTreatmentCommand(
    string ConsultationId,
    string Name,
    int Quantity,
    long UnitPriceSatang,
    string? ServiceId = null,
    string? Note = null);

/// <summary>
/// Records a procedure and, with it, the money it will bill for.
/// <para>
/// The price is taken from the request and stored on the row rather than resolved from the service
/// catalogue at read time. A catalogue price changes; a bill raised in March must still say what
/// was charged in March. Where the price came from is a decision for the caller — usually today's
/// catalogue, sometimes a quoted or package price — and this endpoint records what was actually
/// agreed.
/// </para>
/// </summary>
public sealed class AddTreatmentHandler(
    ConsultationAccess access,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ConsultationDto> HandleAsync(
        AddTreatmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        Consultation? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var consultation = await access.ForStaffAsync(command.ConsultationId, token);

            var treatment = consultation.AddTreatment(
                command.Name,
                command.Quantity,
                command.UnitPriceSatang,
                staffId,
                clock.UtcNow,
                command.ServiceId,
                command.Note);

            audit.Record(new AuditEvent(
                AuditActions.TreatmentAdded,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                consultation.BranchId,
                new Dictionary<string, object?>
                {
                    ["treatment_id"] = treatment.Id,
                    ["name"] = treatment.Name,
                    ["line_total_satang"] = treatment.LineTotal.Satang,
                }));

            updated = consultation;
        }, cancellationToken);

        return ConsultationDto.From(updated!);
    }
}
