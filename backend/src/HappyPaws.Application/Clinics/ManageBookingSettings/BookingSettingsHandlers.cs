using FluentValidation;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.ManageBookingSettings;

/// <summary>
/// <c>GET /clinic/settings/booking</c> — "ช่วงเวลาจอง lead time โควตา walk-in".
/// </summary>
public sealed class GetBookingSettingsHandler(IClinicSettingsStore settings)
{
    public async Task<BookingSettingsDto> HandleAsync(CancellationToken cancellationToken = default) =>
        BookingSettingsDto.From(await settings.BookingAsync(cancellationToken));
}

/// <summary>
/// <c>PATCH /clinic/settings/booking</c> — "slot 15/20/30 นาที · ยกเลิกล่วงหน้ากี่ชั่วโมง". An
/// absent field is left alone.
/// </summary>
public sealed record UpdateBookingSettingsCommand(
    int? SlotMinutes = null,
    int? LeadTimeMinutes = null,
    int? CancellationCutoffHours = null,
    int? MaxAdvanceDays = null,
    int? WalkInSlotsPerHour = null,
    uint? IfMatchVersion = null);

public sealed class UpdateBookingSettingsValidator : AbstractValidator<UpdateBookingSettingsCommand>
{
    public UpdateBookingSettingsValidator()
    {
        // The same bounds the entity enforces, stated here so a bad value answers 422 and names
        // its field instead of surfacing as a domain refusal.
        RuleFor(command => command.SlotMinutes)
            .Must(slot => slot is >= 5 and <= 120 && slot % 5 == 0)
            .WithMessage("ความยาว slot ต้องเป็นจำนวนเท่าของ 5 นาที ระหว่าง 5–120 นาที")
            .When(command => command.SlotMinutes is not null);

        RuleFor(command => command.LeadTimeMinutes).InclusiveBetween(0, 60 * 24 * 30);
        RuleFor(command => command.CancellationCutoffHours).InclusiveBetween(0, 24 * 30);
        RuleFor(command => command.MaxAdvanceDays).InclusiveBetween(1, 365);
        RuleFor(command => command.WalkInSlotsPerHour).InclusiveBetween(0, 60);
    }
}

/// <summary>
/// Changes the clinic's booking rules.
/// <para>
/// A change here takes effect on the next request — the availability feed and the slot resolver
/// read these rows each time rather than caching them. It does not reach back: an appointment
/// booked under last week's lead time stays booked.
/// </para>
/// <para>
/// <b><c>walk_in_slots_per_hour</c> is stored and returned but not enforced.</b> The contract
/// names the setting ("โควตา walk-in") and never the rule it implies — whether the quota is held
/// back from online booking per vet, per branch or per hour of the day — so nothing was invented.
/// See <c>docs/system-overview.md § 5</c>.
/// </para>
/// </summary>
public sealed class UpdateBookingSettingsHandler(
    IClinicSettingsStore settings,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<BookingSettingsDto> HandleAsync(
        UpdateBookingSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        BookingSettings? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var booking = await settings.BookingForUpdateAsync(token);

            ConcurrencyGuard.Ensure(booking, command.IfMatchVersion);

            booking.Update(
                command.SlotMinutes,
                command.LeadTimeMinutes,
                command.CancellationCutoffHours,
                command.MaxAdvanceDays,
                command.WalkInSlotsPerHour);

            updated = booking;
        }, cancellationToken);

        return BookingSettingsDto.From(updated!);
    }
}
