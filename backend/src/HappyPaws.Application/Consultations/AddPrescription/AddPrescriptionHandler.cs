using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.Inventory;
using HappyPaws.Application.Patients;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Inventory;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Consultations.AddPrescription;

/// <summary>
/// <c>POST /staff/consultations/{id}/prescriptions</c> —
/// "ตรวจแพ้ยาและตัดสต๊อกเมื่อจ่ายจริง".
/// <para>
/// <c>AcknowledgeAllergy</c> carries the prescriber's stated reason for writing this drug despite a
/// recorded allergy. It is sent only on a second attempt, after the first was refused.
/// </para>
/// <para>
/// <c>InventoryItemId</c> is not in the API reference (<c>docs/contract-extensions.md § 20</c>): the
/// drug picked from stock, so the dispense takes and charges that item instead of matching the name.
/// With it, <c>DrugName</c> may be left out and the item's name is written; a name sent beside it is
/// kept as written, for the label. Without it, the request is the contract's and behaves as before.
/// </para>
/// </summary>
public sealed record AddPrescriptionCommand(
    string ConsultationId,
    string? DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    int Quantity,
    string? Instructions = null,
    string? AcknowledgeAllergy = null,
    string? InventoryItemId = null);

/// <summary>
/// Only the extension's field is checked here. Everything else is the domain's, as before, so a
/// request without <c>inventory_item_id</c> is refused exactly as it always was.
/// </summary>
public sealed class AddPrescriptionValidator : AbstractValidator<AddPrescriptionCommand>
{
    public AddPrescriptionValidator()
    {
        RuleFor(command => command.InventoryItemId)
            .MaximumLength(40).WithMessage("inventory_item_id ไม่ถูกต้อง")
            .When(command => command.InventoryItemId is not null);
    }
}

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
    RequestValidator validator,
    ConsultationAccess access,
    IPetAllergyRepository allergies,
    IInventoryItemRepository items,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ConsultationDto> HandleAsync(
        AddPrescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        Consultation? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var consultation = await access.ForStaffAsync(command.ConsultationId, token);

            var item = await PickedItemAsync(command.InventoryItemId, token);
            var drugName = string.IsNullOrWhiteSpace(command.DrugName) && item is not null
                ? item.Name
                : command.DrugName ?? string.Empty;

            // A drug picked from stock is checked under both names: as written, and as stocked.
            var conflicts = await FindConflictsAsync(consultation.PetId, [drugName, item?.Name], token);

            if (conflicts.Count > 0)
            {
                Refuse(drugName, command.AcknowledgeAllergy, conflicts);
            }

            var prescription = consultation.AddPrescription(
                staffId,
                clock.UtcNow,
                drugName,
                command.Dose,
                command.Frequency,
                command.DurationDays,
                command.Quantity,
                command.Instructions,
                conflicts.Count > 0 ? command.AcknowledgeAllergy : null,
                item?.Id);

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
                    ["inventory_item_id"] = prescription.InventoryItemId,
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

    /// <summary>
    /// The item the prescriber picked from stock, or null when the drug was typed. An id that names
    /// no item is <c>404</c>; a supply (syringes, gauze) is never prescribed and is <c>422</c>.
    /// A retired item is <c>409 inventory_item_retired</c> (<c>docs/contract-extensions.md § 25</c>);
    /// prescriptions that already name it are untouched and can still be dispensed.
    /// </summary>
    private async Task<InventoryItem?> PickedItemAsync(string? itemId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        var item = await items.FindAsync(itemId.Trim(), cancellationToken)
            ?? throw new NotFoundException("inventory_item", itemId);

        if (item.Kind != InventoryItemKind.Medicine)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["inventory_item_id"] = ["รายการนี้เป็นเวชภัณฑ์ ไม่ใช่ยา สั่งจ่ายไม่ได้"],
            });
        }

        item.EnsureInUse();

        return item;
    }

    private async Task<IReadOnlyList<PetAllergy>> FindConflictsAsync(
        string petId,
        IReadOnlyList<string?> drugNames,
        CancellationToken cancellationToken)
    {
        var recorded = await allergies.ActiveForPetAsync(petId, cancellationToken);
        var names = drugNames.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return [.. recorded.Where(allergy => names.Any(allergy.Matches))];
    }

    private static void Refuse(string drugName, string? acknowledgement, IReadOnlyList<PetAllergy> conflicts)
    {
        var severe = conflicts.Any(allergy => allergy.Severity == AllergySeverity.Severe);
        var acknowledged = !string.IsNullOrWhiteSpace(acknowledgement);

        if (severe || !acknowledged)
        {
            throw new AllergyConflictException(
                drugName,
                [.. conflicts.Select(allergy => allergy.Substance)],
                overridable: !severe);
        }
    }
}
