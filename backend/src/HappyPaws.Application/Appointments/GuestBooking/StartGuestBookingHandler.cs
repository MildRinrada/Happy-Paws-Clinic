using FluentValidation;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Identity.RequestOtp;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Appointments.GuestBooking;

/// <summary>
/// <c>POST /booking/guest/start</c> — hold a time for a visitor who has not signed in, and text
/// their phone a code. Not in the API reference: <c>docs/contract-extensions.md § 27</c>.
/// </summary>
public sealed record StartGuestBookingCommand(
    string Phone,
    string ServiceId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    string? BranchId = null);

public sealed class StartGuestBookingValidator : AbstractValidator<StartGuestBookingCommand>
{
    public StartGuestBookingValidator()
    {
        RuleFor(command => command.Phone)
            .Must(phone => UserAccount.NormalisePhone(phone) is { Length: >= 9 and <= 15 })
            .WithMessage("เบอร์โทรต้องมี 9 ถึง 15 หลัก");

        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.StartsAt).NotEqual(default(DateTimeOffset)).WithMessage("ต้องระบุเวลาเริ่ม");
    }
}

/// <summary>The time now kept for the visitor, and when the code just sent stops working.</summary>
public sealed record GuestBookingStartedDto(SlotHoldDto Hold, DateTimeOffset CodeExpiresAt);

/// <summary>
/// The first half of booking without signing up first.
/// <para>
/// <b>The answer says nothing about the number.</b> A code is sent to every number, the way a
/// sign-up code is, and the reply is the same hold and expiry whether the number is a customer, a
/// member of staff or a stranger. Who the number belongs to is only acted on at
/// <c>POST /booking/guest/verify</c>, after the code has proved the visitor holds it.
/// </para>
/// <para>
/// <b>The hold is the same ten-minute hold the portal uses</b>, written against a marker of the
/// phone instead of a customer id (<see cref="SlotHold.CreateForGuest"/>), so it protects the time
/// while the visitor reads the SMS and types the code. A visitor holds one time at a time: starting
/// again releases the earlier guest hold of the same number before the new one is checked, so going
/// back to pick another time does not leave the first one blocked, and picking the same time again
/// is not refused by the visitor's own hold.
/// </para>
/// <para>
/// The per-number code limit (three in ten minutes) is checked before the hold is taken, so a
/// refused code never leaves a time blocked. The per-address cap on guest holds is the API's rate
/// limit policy, not this handler's.
/// </para>
/// </summary>
public sealed class StartGuestBookingHandler(
    BookingSlotResolver resolver,
    ISlotHoldRepository holds,
    RequestOtpHandler otp,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<GuestBookingStartedDto> HandleAsync(
        StartGuestBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var phone = UserAccount.NormalisePhone(command.Phone)!;
        await otp.EnsureUnderLimitAsync(phone, cancellationToken);

        SlotHold? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;

            foreach (var earlier in await holds.ActiveHeldByAsync(SlotHold.GuestHolderFor(phone), now, token))
            {
                earlier.Release(now);
            }

            // Written before the slot is checked, so the hold just released no longer counts.
            await unitOfWork.SaveChangesAsync(token);

            var slot = await resolver.ResolveAsync(
                new SlotRequest(
                    command.ServiceId,
                    command.StaffId,
                    command.BranchId,
                    command.StartsAt,
                    BypassLeadTime: false,
                    RequireOnlineBookable: true),
                token);

            created = SlotHold.CreateForGuest(
                slot.Branch.Id,
                slot.Service.Id,
                slot.StaffId,
                phone,
                slot.StartsAt,
                slot.EndsAt,
                now);

            holds.Add(created);
        }, cancellationToken);

        var sent = await otp.SendAsync(phone, OtpPurpose.Booking, cancellationToken);

        return new GuestBookingStartedDto(SlotHoldDto.From(created!), sent.ExpiresAt);
    }
}
