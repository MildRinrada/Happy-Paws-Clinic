using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients.MarkPetDeceased;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.UndoPetDeceased;

/// <summary>
/// <c>POST /staff/pets/{id}/deceased/undo</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 29</c>.
/// </summary>
/// <param name="PetId">The patient whose death was recorded by mistake.</param>
/// <param name="Reason">Why, in words someone can read back later. Required.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>; optional, as on the contract's own deceased endpoint.</param>
public sealed record UndoPetDeceasedCommand(string PetId, string? Reason, uint? IfMatchVersion = null);

public sealed class UndoPetDeceasedValidator : AbstractValidator<UndoPetDeceasedCommand>
{
    public UndoPetDeceasedValidator()
    {
        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithMessage("ต้องระบุเหตุผลที่ยกเลิกการบันทึกเสียชีวิต")
            .MaximumLength(500).WithMessage("เหตุผลยาวได้ไม่เกิน 500 ตัวอักษร");
    }
}

/// <summary>A visit the death cancelled, for the counter to rebook by hand.</summary>
/// <param name="Id">The cancelled appointment.</param>
/// <param name="BranchId">Where it was booked.</param>
/// <param name="ServiceId">What it was for.</param>
/// <param name="StaffId">With whom, when a vet was chosen.</param>
/// <param name="StartsAt">When it would have started.</param>
/// <param name="EndsAt">When it would have ended.</param>
/// <param name="HasPassed">True when its time has already gone by; there is nothing left to rebook in that slot.</param>
public sealed record CancelledVisitDto(
    string Id,
    string BranchId,
    string ServiceId,
    string? StaffId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool HasPassed)
{
    public static CancelledVisitDto From(Appointment appointment, DateTimeOffset now) =>
        new(
            appointment.Id,
            appointment.BranchId,
            appointment.ServiceId,
            appointment.StaffId,
            appointment.StartsAt,
            appointment.EndsAt,
            appointment.StartsAt <= now);
}

/// <summary>What undoing the death did, and what it deliberately did not.</summary>
/// <param name="PetId">The patient, open again.</param>
/// <param name="UndoneAt">From this moment the record takes bookings and reminders again.</param>
/// <param name="PreviousDeceasedAt">The date of death that was taken back.</param>
/// <param name="CancelledAppointments">The visits the death cancelled. They stay cancelled.</param>
public sealed record PetDeceasedUndoneResult(
    string PetId,
    DateTimeOffset UndoneAt,
    DateTimeOffset PreviousDeceasedAt,
    IReadOnlyList<CancelledVisitDto> CancelledAppointments);

/// <summary>
/// Takes back a death recorded by mistake — the wrong patient picked at the counter, or a pet that
/// was gravely ill and recovered after someone closed the record too early.
/// <para>
/// <b>Allowed at any time, never silently.</b> A time window would leave a mistake found in week
/// two with only a database edit as the fix, which is worse than the risk it guards against. A
/// reason is required and one audit row keeps it with the date of death that was undone.
/// </para>
/// <para>
/// <b>What the death did is not undone.</b> The visits it cancelled stay cancelled and are returned
/// so the counter can rebook them with the owner: their slots may have gone to someone else, and
/// rebooking without asking the owner would surprise them a second time. No message goes to the
/// owner either; there is no template that would say this well, and this is a phone call. From the
/// moment of the undo the record takes bookings again, and reminders resume from that moment:
/// <see cref="Pet.DeceasedUndoneAt"/> keeps a vaccine or planned session whose reminder fell due
/// while the record was closed from being sent late in one burst.
/// </para>
/// </summary>
public sealed class UndoPetDeceasedHandler(
    IPetRepository pets,
    IAppointmentRepository appointments,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<PetDeceasedUndoneResult> HandleAsync(
        UndoPetDeceasedCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var now = clock.UtcNow;
        var reason = command.Reason!.Trim();
        PetDeceasedUndoneResult? result = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindTrackedByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            ConcurrencyGuard.Ensure(pet, command.IfMatchVersion);

            // Only this death's cancellations: anything a death cancelled before an earlier undo
            // was already handed back then.
            var since = pet.DeceasedUndoneAt;
            var deceasedAt = pet.UndoDeceased(now);

            var cancelled = await appointments.CancelledForPetAsync(
                pet.Id, MarkPetDeceasedHandler.CancellationReason, since, token);

            audit.Record(new AuditEvent(
                AuditActions.PetDeceasedUndone,
                AuditResources.Pet,
                pet.Id,
                pet.Id,
                Details: new Dictionary<string, object?>
                {
                    ["previous_deceased_at"] = deceasedAt,
                    ["reason"] = reason,
                    ["cancelled_appointment_ids"] = cancelled.Select(appointment => appointment.Id).ToList(),
                }));

            result = new PetDeceasedUndoneResult(
                pet.Id,
                now,
                deceasedAt,
                [.. cancelled.Select(appointment => CancelledVisitDto.From(appointment, now))]);
        }, cancellationToken);

        return result!;
    }
}
