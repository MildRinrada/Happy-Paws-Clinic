using FluentValidation;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments.BookAppointment;

/// <summary><c>POST /portal/appointments</c> — "ลูกค้ายืนยันการจอง · ต้องมี Idempotency-Key".</summary>
public sealed record BookAppointmentCommand(
    string PetId,
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null,
    string? HoldId = null,
    string? Reason = null,
    string? Type = null);

public sealed class BookAppointmentValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.StartsAt).NotEqual(default(DateTimeOffset)).WithMessage("ต้องระบุเวลาเริ่ม");
        RuleFor(command => command.Reason).MaximumLength(1000);
    }
}

/// <summary>
/// A customer booking their own pet in.
/// <para>
/// Three checks decide this, in order: the caller cares for the pet, the hold they are redeeming
/// is theirs and still alive, and the slot is genuinely free <i>at the moment of writing</i>. The
/// last one is re-run inside the transaction even when a hold was presented, because a hold is a
/// courtesy rather than a lock — it can expire between the form loading and the submit landing,
/// and a booking that trusted a stale hold would double-book a vet.
/// </para>
/// </summary>
public sealed class BookAppointmentHandler(
    BookingSlotResolver resolver,
    IAppointmentRepository appointments,
    ISlotHoldRepository holds,
    IPetRepository pets,
    PetAccessGuard petAccess,
    IClinicSettingsStore settings,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<AppointmentDto> HandleAsync(
        BookAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var customerId = currentUser.RequireCustomerId();
        await petAccess.EnsureCaresForAsync(command.PetId, cancellationToken);

        var pet = await pets.FindByIdAsync(command.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", command.PetId);

        if (pet.IsDeceased)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["pet_id"] = ["เรกคอร์ดของสัตว์เลี้ยงตัวนี้ถูกปิดแล้ว"] });
        }

        // "กติกาต่างกัน": a type the clinic keeps off the booking page — an emergency, a surgery —
        // is one the customer has to call about, the same answer as a service closed to online booking.
        var type = await settings.RequireTypeAsync(command.Type, cancellationToken);

        if (!type.OnlineBooking)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["type"] = [$"นัดประเภท{type.Name}จองออนไลน์ไม่ได้ กรุณาติดต่อคลินิก"],
            });
        }

        Appointment? appointment = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var hold = await ConsumeHoldAsync(command.HoldId, customerId, token);

            var slot = await resolver.ResolveAsync(
                new SlotRequest(
                    command.ServiceId,
                    command.StaffId ?? hold?.StaffId,
                    command.BranchId ?? hold?.BranchId,
                    command.StartsAt,
                    BypassLeadTime: false,
                    RequireOnlineBookable: true,
                    ExcludeHoldId: hold?.Id,
                    Species: pet.Species,
                    DurationMinutes: type.DurationMinutes,
                    LeadTimeMinutes: type.LeadTimeMinutes),
                token);

            appointment = Appointment.Book(
                slot.Branch.Id,
                customerId,
                pet.Id,
                slot.Service.Id,
                slot.StaffId,
                slot.StartsAt,
                slot.EndsAt,
                command.Reason,
                sourceHoldId: hold?.Id,
                typeCode: type.Code);

            appointments.Add(appointment);
        }, cancellationToken);

        return AppointmentDto.From(appointment!);
    }

    private async Task<SlotHold?> ConsumeHoldAsync(string? holdId, string customerId, CancellationToken cancellationToken)
    {
        if (holdId is not { Length: > 0 })
        {
            // Booking without a hold is allowed — the LINE and staff flows do not go through the
            // web form — so the slot check below is the only guard that always runs.
            return null;
        }

        var hold = await holds.FindByIdAsync(holdId, cancellationToken)
            ?? throw new NotFoundException("hold", holdId);

        // Throws hold_not_yours / hold_expired, both 409 through the domain-exception path.
        hold.Consume(customerId, clock.UtcNow);

        return hold;
    }
}
