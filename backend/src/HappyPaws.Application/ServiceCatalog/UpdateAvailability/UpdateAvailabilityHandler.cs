using FluentValidation;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.ServiceCatalog.Dtos;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.ServiceCatalog.UpdateAvailability;

/// <summary>
/// <c>PATCH /staff/services/{id}/availability</c> — "เปิด/ปิดจองออนไลน์ · จำกัดสาขาหรือช่วงเวลา".
/// An absent field is left alone; an empty list lifts that restriction.
/// </summary>
/// <param name="ServiceId">The service.</param>
/// <param name="OnlineBookingEnabled">Whether customers may book it themselves.</param>
/// <param name="BranchIds">The branches that offer it. Empty for every branch.</param>
/// <param name="BookingWindows">When it may be booked online. Empty for any time a vet is on shift.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record UpdateAvailabilityCommand(
    string ServiceId,
    bool? OnlineBookingEnabled = null,
    IReadOnlyList<string>? BranchIds = null,
    IReadOnlyList<BookingWindowInput>? BookingWindows = null,
    uint? IfMatchVersion = null);

/// <summary>One window as the client sends it: <c>{ "day": "saturday", "from": "09:00", "to": "12:00" }</c>.</summary>
public sealed record BookingWindowInput(string Day, string From, string To);

public sealed class UpdateAvailabilityValidator : AbstractValidator<UpdateAvailabilityCommand>
{
    public UpdateAvailabilityValidator()
    {
        RuleFor(command => command.BookingWindows)
            .Must(windows => windows is null || windows.Count <= 50)
            .WithMessage("กำหนดช่วงเวลาได้ไม่เกิน 50 ช่วง");

        RuleForEach(command => command.BookingWindows).ChildRules(window =>
        {
            window.RuleFor(input => input.Day)
                .Must(value => WireTime.TryParseDay(value, out _))
                .WithMessage("วันต้องเป็น monday ถึง sunday");

            window.RuleFor(input => input.From)
                .Must(value => WireTime.TryParseTime(value, out _))
                .WithMessage("เวลาต้องอยู่ในรูปแบบ HH:mm");

            window.RuleFor(input => input.To)
                .Must(value => WireTime.TryParseTime(value, out _))
                .WithMessage("เวลาต้องอยู่ในรูปแบบ HH:mm");

            window.RuleFor(input => input)
                .Must(input => WireTime.TryParseTime(input.From, out var from)
                    && WireTime.TryParseTime(input.To, out var to)
                    && to > from)
                .OverridePropertyName(nameof(BookingWindowInput.To))
                .WithMessage("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม")
                .When(input => WireTime.TryParseTime(input.From, out _) && WireTime.TryParseTime(input.To, out _));
        });
    }
}

/// <summary>
/// Opens or closes a service for online booking, and narrows where and when it is offered.
/// <para>
/// The three settings bind different audiences, and the difference is the contract's
/// (<see cref="ClinicService.OnlineBookingEnabled"/>): the switch and the time windows limit what
/// a <i>customer</i> can book for themselves, while the branch list limits where the service
/// exists at all. A receptionist can therefore book a Saturday-mornings-only service for a Tuesday
/// by phone, but nobody can book it at a branch that does not offer it.
/// </para>
/// <para>
/// Existing appointments are untouched. Closing online booking stops new self-service bookings; it
/// is not a cancellation.
/// </para>
/// </summary>
public sealed class UpdateAvailabilityHandler(
    IServiceRepository services,
    IBranchRepository branches,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<ServiceDto> HandleAsync(
        UpdateAvailabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        ClinicService? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var service = await services.FindTrackedByIdAsync(command.ServiceId, token)
                ?? throw new NotFoundException("service", command.ServiceId);

            ConcurrencyGuard.Ensure(service, command.IfMatchVersion);

            if (command.BranchIds is { } branchIds)
            {
                var ids = branchIds.Distinct(StringComparer.Ordinal).ToArray();

                if (ids.Length > 0 && (await branches.MissingAsync(ids, token)) is { Count: > 0 } missing)
                {
                    throw new ValidationFailedException(new Dictionary<string, string[]>
                    {
                        ["branch_ids"] = [$"ไม่พบสาขา: {string.Join(", ", missing)}"],
                    });
                }

                service.RestrictToBranches(ids);
            }

            if (command.BookingWindows is { } windows)
            {
                service.SetBookingWindows(windows.Select(ToWindow));
            }

            if (command.OnlineBookingEnabled is { } enabled)
            {
                service.SetOnlineBooking(enabled);
            }

            updated = service;
        }, cancellationToken);

        return ServiceDto.From(updated!, DateOnly.FromDateTime(clock.NowInClinicTime().DateTime));
    }

    /// <summary>The validator has already vouched for every value parsed here.</summary>
    private static BookingWindow ToWindow(BookingWindowInput input)
    {
        WireTime.TryParseDay(input.Day, out var day);
        WireTime.TryParseTime(input.From, out var from);
        WireTime.TryParseTime(input.To, out var to);

        return new BookingWindow(day, from, to);
    }
}
