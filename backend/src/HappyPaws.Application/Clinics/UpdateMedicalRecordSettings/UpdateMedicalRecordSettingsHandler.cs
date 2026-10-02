using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Clinics.UpdateMedicalRecordSettings;

/// <summary>
/// <c>PATCH /clinic/settings/medical-record</c> — "ฟิลด์บังคับ แบบฟอร์มตรวจ การ sign". An absent
/// field is left alone; an empty list clears that setting.
/// </summary>
public sealed record UpdateMedicalRecordSettingsCommand(
    IReadOnlyList<string>? RequiredFields = null,
    IReadOnlyList<string>? ExaminationSystems = null,
    IReadOnlyList<string>? OwnerVisibleSections = null,
    bool? SignerMustBeCaseVet = null,
    uint? IfMatchVersion = null);

public sealed class UpdateMedicalRecordSettingsValidator : AbstractValidator<UpdateMedicalRecordSettingsCommand>
{
    public UpdateMedicalRecordSettingsValidator()
    {
        RuleForEach(command => command.RequiredFields)
            .Must(field => ConsultationRequirements.All.Contains(field))
            .WithMessage($"ฟิลด์บังคับต้องเป็นหนึ่งใน: {string.Join(", ", ConsultationRequirements.All)}");

        RuleForEach(command => command.ExaminationSystems).NotEmpty().MaximumLength(60);

        RuleFor(command => command.ExaminationSystems)
            .Must(systems => systems is null || systems.Count <= 40)
            .WithMessage("แบบฟอร์มตรวจมีได้ไม่เกิน 40 ระบบ");

        RuleForEach(command => command.OwnerVisibleSections)
            .Must(section => MedicalRecordSettings.RecordSections.Contains(section))
            .WithMessage($"ส่วนของเวชระเบียนต้องเป็นหนึ่งใน: {string.Join(", ", MedicalRecordSettings.RecordSections)}");
    }
}

/// <summary>
/// Changes what a record must contain, what the examination form offers, and who may sign.
/// <para>
/// Audited, unlike the other settings in this module. Opening hours and a queue prefix change how
/// the clinic looks; this changes which clinical documents can be signed and by whom, and "when
/// did we stop requiring vitals" is a question a clinical-governance review will ask.
/// </para>
/// <para>
/// Nothing here reaches back. A record signed before a field became required stays signed — a
/// signed record is immutable, and a setting is not an amendment.
/// </para>
/// <para>
/// Like the queue settings, the contract gives this a <c>PATCH</c> and no <c>GET</c>; an empty
/// body reads the settings back without writing.
/// </para>
/// </summary>
public sealed class UpdateMedicalRecordSettingsHandler(
    IClinicSettingsStore settings,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<MedicalRecordSettingsDto> HandleAsync(
        UpdateMedicalRecordSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        if (command is
            {
                RequiredFields: null,
                ExaminationSystems: null,
                OwnerVisibleSections: null,
                SignerMustBeCaseVet: null,
            })
        {
            return MedicalRecordSettingsDto.From(await settings.MedicalRecordAsync(cancellationToken));
        }

        MedicalRecordSettings? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var record = await settings.MedicalRecordForUpdateAsync(token);

            ConcurrencyGuard.Ensure(record, command.IfMatchVersion);

            var before = Snapshot(record);

            record.Update(
                command.RequiredFields,
                command.ExaminationSystems,
                command.OwnerVisibleSections,
                command.SignerMustBeCaseVet);

            audit.Record(new AuditEvent(
                AuditActions.MedicalRecordSettingsChanged,
                AuditResources.Settings,
                record.Id,
                Details: new Dictionary<string, object?>
                {
                    ["before"] = before,
                    ["after"] = Snapshot(record),
                }));

            updated = record;
        }, cancellationToken);

        return MedicalRecordSettingsDto.From(updated!);
    }

    /// <summary>Copies, not references — the lists are about to be mutated in place.</summary>
    private static Dictionary<string, object?> Snapshot(MedicalRecordSettings record) =>
        new()
        {
            ["required_fields"] = record.RequiredFields.ToArray(),
            ["examination_systems"] = record.ExaminationSystems.ToArray(),
            ["owner_visible_sections"] = record.OwnerVisibleSections.ToArray(),
            ["signer_must_be_case_vet"] = record.SignerMustBeCaseVet,
        };
}
