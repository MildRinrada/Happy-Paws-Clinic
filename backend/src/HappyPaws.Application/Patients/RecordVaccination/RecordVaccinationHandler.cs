using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Consultations;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.RecordVaccination;

/// <summary>
/// <c>POST /staff/pets/{id}/vaccinations</c> — "ล็อตวัคซีน วันหมดอายุ · ตั้งรอบถัดไปอัตโนมัติ".
/// <para>
/// <c>AdministeredAt</c> defaults to now; a dose given last week can be entered late.
/// <c>NextDueOn</c> is an explicit due date; <c>ValidForDays</c> is how long the dose protects for
/// when no date is given, and zero means no further dose.
/// </para>
/// </summary>
public sealed record RecordVaccinationCommand(
    string PetId,
    string VaccineName,
    string LotNumber,
    DateOnly VialExpiresOn,
    DateTimeOffset? AdministeredAt = null,
    DateOnly? NextDueOn = null,
    int? ValidForDays = null,
    string? Manufacturer = null,
    string? ConsultationId = null,
    string? Note = null);

public sealed class RecordVaccinationValidator : AbstractValidator<RecordVaccinationCommand>
{
    public RecordVaccinationValidator()
    {
        RuleFor(command => command.VaccineName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.LotNumber).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Manufacturer).MaximumLength(200);
        RuleFor(command => command.Note).MaximumLength(1_000);

        RuleFor(command => command.ValidForDays)
            .InclusiveBetween(0, Vaccination.MaxValidForDays);

        RuleFor(command => command.NextDueOn)
            .Null().WithMessage("ส่ง next_due_on หรือ valid_for_days อย่างใดอย่างหนึ่ง")
            .When(command => command.ValidForDays is not null);
    }
}

/// <summary>
/// Records a dose and sets when the next one is due.
/// <para>
/// <b>"ตั้งรอบถัดไปอัตโนมัติ"</b> — the vet may give an exact date or a length of cover; with
/// neither, the next dose is due a year later, which is right for every core vaccine a small-animal
/// clinic gives annually. The due date is stored on the dose, where the reminder rules that land
/// with the Automation module will read it ("ครบกำหนดวัคซีน 14 วัน → ส่ง LINE" is the contract's
/// own example of such a rule).
/// </para>
/// <para>
/// A dose for a patient marked deceased is refused: the record is closed, and the due date it would
/// set is a reminder nobody should send.
/// </para>
/// </summary>
public sealed class RecordVaccinationHandler(
    IPetRepository pets,
    IVaccinationRepository vaccinations,
    IConsultationRepository consultations,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    ClinicDays clinicDays,
    IClock clock)
{
    /// <summary>Clock drift between the tablet in the exam room and the server is not a future dose.</summary>
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    public async Task<VaccinationDto> HandleAsync(
        RecordVaccinationCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;
        var administeredAt = command.AdministeredAt ?? now;

        if (administeredAt > now + FutureTolerance)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["administered_at"] = ["บันทึกการฉีดล่วงหน้าไม่ได้"],
            });
        }

        Vaccination? recorded = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            if (pet.IsDeceased)
            {
                throw new StateConflictException(nameof(Pet), "deceased", "record_vaccination");
            }

            if (command.ConsultationId is { Length: > 0 } consultationId)
            {
                var consultation = await consultations.FindByIdAsync(consultationId, token);

                // A dose filed against another patient's visit is a slip that would put it in the
                // wrong medical history. Answered as "no such visit" for this patient.
                if (consultation is null || consultation.PetId != pet.Id)
                {
                    throw new NotFoundException("consultation", consultationId);
                }
            }

            var dose = Vaccination.Record(
                pet.Id,
                command.VaccineName,
                command.LotNumber,
                command.VialExpiresOn,
                administeredAt,
                clinicDays.DateOf(administeredAt),
                staffId,
                command.NextDueOn,
                command.ValidForDays,
                command.Manufacturer,
                command.ConsultationId,
                command.Note);

            vaccinations.Add(dose);

            audit.Record(new AuditEvent(
                AuditActions.VaccinationRecorded,
                AuditResources.Vaccination,
                dose.Id,
                pet.Id,
                Details: new Dictionary<string, object?>
                {
                    ["vaccine_name"] = dose.VaccineName,
                    ["lot_number"] = dose.LotNumber,
                    ["next_due_on"] = dose.NextDueOn,
                }));

            recorded = dose;
        }, cancellationToken);

        return VaccinationDto.From(recorded!);
    }
}
