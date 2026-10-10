using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Consultations;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.StaffAi.Privacy;
using HappyPaws.Application.StaffAi.Prompts;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.StaffAi.SoapDraft;

/// <summary><c>POST /staff/ai/consultations/{id}/soap-draft</c>: <c>{ "notes": "..." }</c>.</summary>
public sealed record DraftSoapCommand(string ConsultationId, string? Notes);

public sealed class DraftSoapValidator : AbstractValidator<DraftSoapCommand>
{
    public const int MaxNotesLength = 4000;

    public DraftSoapValidator()
    {
        RuleFor(command => command.Notes)
            .Must(notes => !string.IsNullOrWhiteSpace(notes)).WithMessage("ต้องพิมพ์บันทึกย่อก่อน")
            .Must(notes => notes is null || notes.Trim().Length <= MaxNotesLength)
            .WithMessage($"บันทึกย่อยาวได้ไม่เกิน {MaxNotesLength} ตัวอักษร");
    }
}

/// <summary>
/// The draft, shaped like the form (<c>draft</c>), and what in it the notes do not support
/// (<c>warnings</c>). Nothing is saved: the vet puts it into the form and saves from there.
/// </summary>
public sealed record SoapDraftDto(string ConsultationId, SoapDraftFieldsDto Draft, IReadOnlyList<DraftWarningDto> Warnings);

/// <summary>
/// Rearranges a vet's short notes into the consultation's SOAP fields and examination lines.
/// <para>
/// <b>Gate.</b> <c>consult:write</c> and <c>ai:use</c> at the route, then the case's branch
/// (<see cref="ConsultationAccess"/>), like every write on the case. A closed case answers
/// <c>409 state_conflict</c>: its record is signed and a draft for it has nowhere to go.
/// </para>
/// <para>
/// <b>What the model sees.</b> The notes, masked; the patient's name, species, breed, sex and
/// age; and the names of the clinic's examination form. Not what is already on the case.
/// </para>
/// <para>
/// <b>Adding nothing.</b> The prompt forbids any diagnosis, drug, dose or finding not in the
/// notes; <see cref="UnverifiedTerms"/> then flags whatever still looks like one.
/// </para>
/// </summary>
public sealed class DraftSoapHandler(
    ConsultationAccess access,
    IPetRepository pets,
    IStaffAiReader reader,
    IClinicSettingsStore settings,
    StaffAiGateway gateway,
    ClinicDays days,
    RequestValidator validator,
    IAuditTrail audit,
    ILogger<DraftSoapHandler> logger)
{
    public async Task<SoapDraftDto> HandleAsync(DraftSoapCommand command, CancellationToken cancellationToken = default)
    {
        gateway.EnsureEnabled();
        await validator.ValidateAsync(command, cancellationToken);

        var consultation = await access.ForStaffAsync(command.ConsultationId, cancellationToken);

        if (consultation.Status != ConsultationStatus.Draft)
        {
            throw new ConflictException(ErrorCodes.StateConflict, "เคสนี้ปิดแล้ว ร่างบันทึกการตรวจให้เคสที่ปิดแล้วไม่ได้");
        }

        var pet = await pets.FindByIdAsync(consultation.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", consultation.PetId);

        var form = await settings.MedicalRecordAsync(cancellationToken);
        var systems = form.ExaminationSystems.Count > 0 ? form.ExaminationSystems : SoapDraftPrompt.DefaultBodySystems;

        var pseudonymizer = new Pseudonymizer();
        if (await reader.CustomerAsync(consultation.CustomerId, cancellationToken) is { } owner)
        {
            pseudonymizer.Person(owner.Name, PersonalKind.Owner);
            pseudonymizer.Register(owner.Phone, PersonalKind.Phone);
            pseudonymizer.Register(owner.Email, PersonalKind.Email);
        }

        var notes = command.Notes!.Trim();
        var patient = PatientDescription.Of(pet, days.Today);

        var answer = await gateway.CompleteTextAsync(
            StaffAiFeatures.SoapDraft,
            SoapDraftPrompt.System(systems),
            StaffAiPromptRules.Fence($"{patient}\nVet's notes:\n{pseudonymizer.Mask(notes)}"),
            cancellationToken);

        var parsed = SoapDraftParser.Parse(answer, systems);

        if (parsed is null)
        {
            logger.LogWarning("Staff AI SOAP draft for {ConsultationId} was not usable JSON", consultation.Id);
            throw new AiUnavailableException();
        }

        var draft = new SoapDraftFieldsDto(
            pseudonymizer.Restore(parsed.ChiefComplaint),
            pseudonymizer.Restore(parsed.Subjective),
            pseudonymizer.Restore(parsed.Objective),
            pseudonymizer.Restore(parsed.Assessment),
            pseudonymizer.Restore(parsed.Plan),
            [.. parsed.Examination.Select(finding => finding with { Note = finding.Note is null ? null : pseudonymizer.Restore(finding.Note) })]);

        var warnings = UnverifiedTerms.Find(
            [
                ("chief_complaint", draft.ChiefComplaint),
                ("subjective", draft.Subjective),
                ("objective", draft.Objective),
                ("assessment", draft.Assessment),
                ("plan", draft.Plan),
                .. draft.Examination.Select(finding => ("examination", $"{finding.BodySystem} {finding.Note}")),
            ],
            $"{patient}\n{notes}\n{string.Join("\n", systems)}");

        await audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.AiSoapDraft,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                consultation.BranchId,
                new Dictionary<string, object?> { ["warnings"] = warnings.Count, ["findings"] = draft.Examination.Count }),
            cancellationToken);

        return new SoapDraftDto(consultation.Id, draft, warnings);
    }
}
