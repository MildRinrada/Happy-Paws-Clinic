using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.Patients;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Consultations.AddPrescription;

/// <summary>
/// <c>POST /staff/consultations/{id}/prescriptions</c> —
/// "ตรวจแพ้ยาและตัดสต๊อกเมื่อจ่ายจริง".
/// <para>
/// <c>AcknowledgeAllergy</c> carries the prescriber's stated reason for writing this drug despite a
/// recorded allergy. It is sent only on a second attempt, after the first was refused.
/// </para>
/// </summary>
public sealed record AddPrescriptionCommand(
    string ConsultationId,
    string DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    int Quantity,
    string? Instructions = null,
    string? AcknowledgeAllergy = null);

/// <summary>
/// Writes a drug on the case, after checking the patient's recorded allergies.
/// <para>
/// The check is the contract's ("ตรวจแพ้ยา") and its behaviour is this project's decision, recorded
/// in <c>docs/assumptions.md § 19</c>. A match refuses the request with <c>409 allergy_conflict</c>
/// and names the allergy; the vet may resend with an acknowledgement, which is stored on the
/// prescription and audited. A <b>severe</b> allergy is not overridable through this endpoint at
/// all — that decision needs a conversation, not a retry.
/// </para>
/// <para>
/// What the check cannot do is worth stating plainly: matching is by name, so it knows nothing
/// about drug classes. An allergy to penicillin will not flag amoxicillin. Closing that gap needs a
/// drug database this system does not have, and pretending otherwise would be worse than the
/// honest limit.
/// </para>
/// <para>
/// Stock is untouched here. "ตัดสต๊อกเมื่อจ่ายจริง" — inventory moves when the drug is actually
/// handed over, under <c>rx:dispense</c>.
/// </para>
/// </summary>
public sealed class AddPrescriptionHandler(
    ConsultationAccess access,
    IPetAllergyRepository allergies,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ConsultationDto> HandleAsync(
        AddPrescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        Consultation? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var consultation = await access.ForStaffAsync(command.ConsultationId, token);

            var conflicts = await FindConflictsAsync(consultation.PetId, command.DrugName, token);

            if (conflicts.Count > 0)
            {
                Refuse(command, conflicts);
            }

            var prescription = consultation.AddPrescription(
                staffId,
                clock.UtcNow,
                command.DrugName,
                command.Dose,
                command.Frequency,
                command.DurationDays,
                command.Quantity,
                command.Instructions,
                conflicts.Count > 0 ? command.AcknowledgeAllergy : null);

            audit.Record(new AuditEvent(
                AuditActions.PrescriptionAdded,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                consultation.BranchId,
                new Dictionary<string, object?>
                {
                    ["prescription_id"] = prescription.Id,
                    ["drug_name"] = prescription.DrugName,
                }));

            if (conflicts.Count > 0)
            {
                // A separate row from the prescription itself, because this is the line a reviewer
                // searches for: a known allergy was overridden, by whom, and with what reason.
                audit.Record(new AuditEvent(
                    AuditActions.AllergyOverridden,
                    AuditResources.Consultation,
                    consultation.Id,
                    consultation.PetId,
                    consultation.BranchId,
                    new Dictionary<string, object?>
                    {
                        ["prescription_id"] = prescription.Id,
                        ["drug_name"] = prescription.DrugName,
                        ["allergies"] = conflicts.Select(allergy => allergy.Substance).ToArray(),
                        ["acknowledgement"] = command.AcknowledgeAllergy,
                    }));
            }

            updated = consultation;
        }, cancellationToken);

        return ConsultationDto.From(updated!);
    }

    private async Task<IReadOnlyList<PetAllergy>> FindConflictsAsync(
        string petId,
        string drugName,
        CancellationToken cancellationToken)
    {
        var recorded = await allergies.ActiveForPetAsync(petId, cancellationToken);

        return [.. recorded.Where(allergy => allergy.Matches(drugName))];
    }

    private static void Refuse(AddPrescriptionCommand command, IReadOnlyList<PetAllergy> conflicts)
    {
        var severe = conflicts.Any(allergy => allergy.Severity == AllergySeverity.Severe);
        var acknowledged = !string.IsNullOrWhiteSpace(command.AcknowledgeAllergy);

        if (severe || !acknowledged)
        {
            throw new AllergyConflictException(
                command.DrugName,
                [.. conflicts.Select(allergy => allergy.Substance)],
                overridable: !severe);
        }
    }
}
