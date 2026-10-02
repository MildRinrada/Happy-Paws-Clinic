using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.MedicalRecords;

namespace HappyPaws.Application.MedicalRecords.GenerateDocument;

/// <summary>
/// <c>POST /staff/documents/generate</c> — "ใบรับรองสุขภาพ ใบส่งตัว สมุดวัคซีน เป็น PDF".
/// <para>
/// <c>Type</c> is <c>health_certificate</c>, <c>referral_letter</c> or <c>vaccination_book</c>.
/// A referral names <c>ReferTo</c> and <c>Reason</c>. <c>ConsultationId</c> picks the visit to draw
/// on; without it, the patient's latest signed record is used.
/// </para>
/// </summary>
public sealed record GenerateDocumentCommand(
    string Type,
    string PetId,
    string? ConsultationId = null,
    string? ReferTo = null,
    string? Reason = null,
    string? Remarks = null);

public sealed class GenerateDocumentValidator : AbstractValidator<GenerateDocumentCommand>
{
    public GenerateDocumentValidator()
    {
        RuleFor(command => command.Type)
            .Must(type => GenerateDocumentHandler.Types.Contains(type))
            .WithMessage($"type ต้องเป็น {string.Join(", ", GenerateDocumentHandler.Types)}");

        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.ReferTo).NotEmpty().MaximumLength(200).When(command => command.Type == "referral_letter");
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1_000).When(command => command.Type == "referral_letter");
        RuleFor(command => command.Remarks).MaximumLength(1_000);
    }
}

/// <summary>The PDF, and the name to save it under.</summary>
public sealed record GeneratedDocumentDto(string FileName, byte[] Content);

/// <summary>
/// Prints a document from a patient's file.
/// <para>
/// <b>A certificate and a referral are signed by whoever prints them</b>, so they can only be
/// printed by someone with a professional licence number on their staff record. The scope the
/// contract gives, <c>record:read</c>, says who may read the file; the licence says who may put a
/// name to a statement about the animal's health. A vaccination book only copies what is recorded,
/// and anyone with the scope may print it.
/// </para>
/// <para>
/// Every document is written to the access log: a copy of health data has left the system on paper.
/// Nothing is stored. The same request prints the same document again, as of the moment it is asked.
/// </para>
/// </summary>
public sealed class GenerateDocumentHandler(
    IPetRepository pets,
    ICustomerRepository customers,
    IStaffRepository staff,
    IBranchRepository branches,
    IClinicSettingsStore settings,
    IMedicalRecordRepository records,
    IPetTimelineReader timeline,
    IPetAllergyRepository allergies,
    IVaccinationRepository vaccinations,
    IDocumentRenderer renderer,
    IAuditTrail audit,
    ICurrentUser currentUser,
    RequestValidator validator,
    ClinicDays clinicDays,
    IClock clock)
{
    public static readonly IReadOnlyList<string> Types = ["health_certificate", "referral_letter", "vaccination_book"];

    public async Task<GeneratedDocumentDto> HandleAsync(GenerateDocumentCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var signerRow = await staff.FindRowAsync(staffId, cancellationToken)
            ?? throw new ForbiddenException("caller has no staff record");

        if (command.Type != "vaccination_book" && string.IsNullOrWhiteSpace(signerRow.Membership.LicenseNo))
        {
            throw new ForbiddenException("ใบรับรองและใบส่งตัวต้องออกโดยผู้มีเลขใบอนุญาตประกอบวิชาชีพ");
        }

        var pet = await pets.FindByIdAsync(command.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", command.PetId);

        var record = await RecordAsync(command, pet.Id, cancellationToken);

        if (command.Type == "referral_letter" && record is null)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["consultation_id"] = ["ใบส่งตัวต้องอ้างอิงการตรวจที่ลงนามแล้ว สัตว์ตัวนี้ยังไม่มี"],
            });
        }

        var context = new DocumentContext(
            pet,
            await customers.FindByIdAsync(pet.PrimaryCustomerId, cancellationToken),
            await settings.ProfileAsync(cancellationToken),
            (await branches.ListActiveAsync(cancellationToken)).FirstOrDefault(branch =>
                branch.Id == (record?.BranchId ?? currentUser.BranchIds.FirstOrDefault())),
            new PrintableSignature(signerRow.Account.DisplayName, signerRow.Membership.LicenseNo, command.Type == "vaccination_book" ? "ผู้พิมพ์" : "สัตวแพทย์ผู้ตรวจ"),
            clinicDays.Today,
            record?.Id,
            record is null ? null : RecordContent.Read(record.CurrentContentJson),
            await timeline.LatestWeightAsync(pet.Id, cancellationToken),
            await allergies.ActiveForPetAsync(pet.Id, cancellationToken),
            await vaccinations.ForPetAsync(pet.Id, cancellationToken),
            clock.ClinicTimeZone);

        var reference = EntityId.New("doc");

        var document = command.Type switch
        {
            "health_certificate" => DocumentTemplates.HealthCertificate(context, command.Remarks, reference),
            "referral_letter" => DocumentTemplates.ReferralLetter(context, command.ReferTo!, command.Reason!, reference),
            _ => DocumentTemplates.VaccinationBook(context, reference),
        };

        var pdf = renderer.RenderPdf(document);

        await audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.DocumentGenerated,
                AuditResources.Pet,
                pet.Id,
                pet.Id,
                record?.BranchId,
                new Dictionary<string, object?>
                {
                    ["type"] = command.Type,
                    ["reference"] = reference,
                    ["record_id"] = record?.Id,
                }),
            cancellationToken);

        return new GeneratedDocumentDto($"{command.Type}-{pet.HospitalNumber}-{context.Today:yyyyMMdd}.pdf", pdf);
    }

    private async Task<MedicalRecord?> RecordAsync(GenerateDocumentCommand command, string petId, CancellationToken cancellationToken)
    {
        if (command.ConsultationId is { Length: > 0 } consultationId)
        {
            var record = await records.FindByConsultationAsync(consultationId, cancellationToken);

            return record is not null && record.PetId == petId
                ? record
                : throw new NotFoundException("consultation", consultationId);
        }

        return (await records.ForPetAsync(petId, 1, cancellationToken)).FirstOrDefault();
    }
}
