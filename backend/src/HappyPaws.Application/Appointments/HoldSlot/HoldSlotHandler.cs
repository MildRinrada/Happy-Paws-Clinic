using FluentValidation;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments.HoldSlot;

/// <summary><c>POST /booking/holds</c> — "จองค้างไว้ 10 นาทีระหว่างกรอกข้อมูล".</summary>
public sealed record HoldSlotCommand(
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null);

public sealed class HoldSlotValidator : AbstractValidator<HoldSlotCommand>
{
    public HoldSlotValidator()
    {
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.StartsAt).NotEqual(default(DateTimeOffset)).WithMessage("ต้องระบุเวลาเริ่ม");
    }
}

/// <summary>
/// Takes a slot off the board while the customer fills in the rest of the form.
/// <para>
/// The hold is checked and written in one transaction. Without that, two customers who both pass
/// the availability check a few milliseconds apart both get a hold on the same slot, and the
/// mechanism that exists to prevent a collision has produced one.
/// </para>
/// <para>
/// A hold is not a booking and carries no commitment: it expires on its own in ten minutes, so an
/// abandoned form costs the clinic ten minutes of one vet's calendar and nothing else.
/// </para>
/// </summary>
public sealed class HoldSlotHandler(
    BookingSlotResolver resolver,
    ISlotHoldRepository holds,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<SlotHoldDto> HandleAsync(HoldSlotCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var customerId = currentUser.RequireCustomerId();
        SlotHold? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var slot = await resolver.ResolveAsync(
                new SlotRequest(
                    command.ServiceId,
                    command.StaffId,
                    command.BranchId,
                    command.StartsAt,
                    BypassLeadTime: false,
                    RequireOnlineBookable: true),
                token);

            created = SlotHold.Create(
                slot.Branch.Id,
                slot.Service.Id,
                slot.StaffId,
                customerId,
                slot.StartsAt,
                slot.EndsAt,
                clock.UtcNow);

            holds.Add(created);
        }, cancellationToken);

        return SlotHoldDto.From(created!);
    }
}
