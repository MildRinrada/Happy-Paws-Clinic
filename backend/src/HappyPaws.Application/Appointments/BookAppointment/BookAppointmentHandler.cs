using FluentValidation;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Patients.CreatePet;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Appointments.BookAppointment;

/// <summary>
/// <c>POST /portal/appointments</c> — "ลูกค้ายืนยันการจอง · ต้องมี Idempotency-Key".
/// <para>
/// <c>NewPet</c> is not in the API reference (<c>docs/contract-extensions.md § 27</c>): it books a
/// pet the clinic has not met yet, registering it in the same transaction. Exactly one of
/// <c>PetId</c> and <c>NewPet</c>.
/// </para>
/// </summary>
public sealed record BookAppointmentCommand(
    string? PetId,
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null,
    string? HoldId = null,
    string? Reason = null,
    string? Type = null,
    NewPetInput? NewPet = null);

/// <summary>A pet booked in for the first time: the two things the counter needs to open its record.</summary>
public sealed record NewPetInput(string Name, string Species);

public sealed class BookAppointmentValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(command => command.PetId)
            .Must((command, petId) => string.IsNullOrWhiteSpace(petId) != (command.NewPet is null))
            .WithMessage("ระบุ pet_id หรือ new_pet อย่างใดอย่างหนึ่ง");

        RuleFor(command => command.NewPet!.Name).NotEmpty().MaximumLength(120).When(command => command.NewPet is not null);
        RuleFor(command => command.NewPet!.Species).NotEmpty().MaximumLength(60).When(command => command.NewPet is not null);
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
    CreatePetHandler createPet,
    IClinicSettingsStore settings,
    ICurrentUser currentUser,
    IWebhookOutbox webhooks,
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
        var pet = command.NewPet is null ? await ExistingPetAsync(command.PetId!, cancellationToken) : null;

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
                    Species: pet?.Species ?? command.NewPet!.Species.Trim(),
                    DurationMinutes: type.DurationMinutes,
                    LeadTimeMinutes: type.LeadTimeMinutes),
                token);

            // Registered only once the slot is known to be free, inside the same transaction, so a
            // refused booking leaves no stray patient behind.
            var petId = pet?.Id
                ?? (await createPet.HandleAsync(new CreatePetCommand(command.NewPet!.Name, command.NewPet.Species, customerId), token)).Id;

            appointment = Appointment.Book(
                slot.Branch.Id,
                customerId,
                petId,
                slot.Service.Id,
                slot.StaffId,
                slot.StartsAt,
                slot.EndsAt,
                command.Reason,
                sourceHoldId: hold?.Id,
                typeCode: type.Code);

            appointments.Add(appointment);

            webhooks.Enqueue(WebhookEventTypes.AppointmentCreated, WebhookPayloads.Appointment(appointment));
        }, cancellationToken);

        return AppointmentDto.From(appointment!);
    }

    private async Task<Domain.Patients.Pet> ExistingPetAsync(string petId, CancellationToken cancellationToken)
    {
        await petAccess.EnsureCaresForAsync(petId, cancellationToken);

        var pet = await pets.FindByIdAsync(petId, cancellationToken)
            ?? throw new NotFoundException("pet", petId);

        if (pet.IsDeceased)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["pet_id"] = ["เรกคอร์ดของสัตว์เลี้ยงตัวนี้ถูกปิดแล้ว"] });
        }

        return pet;
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
