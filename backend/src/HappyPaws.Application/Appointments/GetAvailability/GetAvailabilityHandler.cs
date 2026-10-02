using FluentValidation;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;

namespace HappyPaws.Application.Appointments.GetAvailability;

/// <summary><c>GET /booking/availability?service=&amp;date=&amp;vet=&amp;branch=</c>.</summary>
public sealed record GetAvailabilityQuery(
    string ServiceId,
    DateOnly Date,
    string? StaffId = null,
    string? BranchId = null);

public sealed class GetAvailabilityValidator : AbstractValidator<GetAvailabilityQuery>
{
    public GetAvailabilityValidator()
    {
        RuleFor(query => query.ServiceId).NotEmpty();
        RuleFor(query => query.Date).NotEqual(default(DateOnly)).WithMessage("ต้องระบุวันที่");
    }
}

/// <summary>
/// The public booking feed: which openings exist for a service on a day.
/// <para>
/// Public, per the contract, and therefore written as a read that discloses nothing beyond what a
/// booking page must show — times and the vet on them. It offers only services the clinic has
/// opened for online booking, so an unauthenticated caller cannot use it to enumerate the internal
/// theatre list or infer a surgeon's schedule. Staff get the full internal view from
/// <c>GET /staff/schedules/availability</c>.
/// </para>
/// </summary>
public sealed class GetAvailabilityHandler(
    IBookingCatalog catalog,
    IAppointmentRepository appointments,
    ISlotHoldRepository holds,
    BookingSlotResolver resolver,
    RequestValidator validator,
    IClock clock)
{
    public async Task<AvailabilityDto> HandleAsync(
        GetAvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(query, cancellationToken);

        var settings = await catalog.GetBookingSettingsAsync(cancellationToken);

        var service = await catalog.FindServiceAsync(query.ServiceId, cancellationToken)
            ?? throw new NotFoundException("service", query.ServiceId);

        if (!service.IsActive || !service.OnlineBookingEnabled)
        {
            // Same answer for "no such service" and "not offered online", so the feed cannot be
            // used to discover which internal services exist.
            throw new NotFoundException("service", query.ServiceId);
        }

        var branch = query.BranchId is { Length: > 0 }
            ? await catalog.FindBranchAsync(query.BranchId, cancellationToken)
            : await catalog.SoleActiveBranchAsync(cancellationToken);

        if (branch is null || !branch.IsActive)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["branch"] = ["ต้องระบุสาขาที่เปิดให้บริการ"] });
        }

        if (!service.IsOfferedAt(branch.Id))
        {
            // Same answer again: a service this branch does not offer is, from here, not a service.
            throw new NotFoundException("service", query.ServiceId);
        }

        if (await catalog.HolidayAsync(branch.Id, query.Date, cancellationToken) is not null)
        {
            // "ปิดรับนัดอัตโนมัติ". A closed day is a true answer, not an error: nothing is
            // available, and the feed says exactly that.
            return new AvailabilityDto(branch.Id, service.Id, query.Date, service.DurationMinutes, []);
        }

        var (from, to) = resolver.ClinicDayBounds(ToClinicInstant(query.Date));

        var shifts = await catalog.ShiftsAsync(branch.Id, from, to, query.StaffId, cancellationToken);
        var staffIds = shifts.Select(shift => shift.StaffId).Distinct(StringComparer.Ordinal).ToArray();

        // Bounded to the day that was asked for, at both ends. A shift can run past midnight — a
        // night rota, or simply a long Saturday — and without the clamp the feed for Monday would
        // quietly include Tuesday's small hours.
        var earliest = Later(from, clock.UtcNow.AddMinutes(settings.LeadTimeMinutes));
        var latest = Earlier(to.AddTicks(-1), clock.UtcNow.AddDays(settings.MaxAdvanceDays));

        var slots = SlotCalculator.Enumerate(new SlotQuery(
            shifts,
            await appointments.OccupyingSlotsAsync(branch.Id, staffIds, from, to, null, cancellationToken),
            await holds.ActiveInWindowAsync(branch.Id, staffIds, from, to, clock.UtcNow, null, cancellationToken),
            settings.SlotMinutes,
            service.DurationMinutes,
            earliest,
            latest,
            service.EligibleStaffIds,

            // This feed is the customer's, so the service's online windows always apply.
            start => service.IsBookableOnlineAt(resolver.ClinicLocal(start))));

        return new AvailabilityDto(
            branch.Id,
            service.Id,
            query.Date,
            service.DurationMinutes,
            [.. slots.Select(AvailableSlotDto.From)]);
    }

    private static DateTimeOffset Later(DateTimeOffset first, DateTimeOffset second) =>
        first > second ? first : second;

    private static DateTimeOffset Earlier(DateTimeOffset first, DateTimeOffset second) =>
        first < second ? first : second;

    /// <summary>
    /// Anchors a bare calendar date to the clinic's time zone. <c>?date=2026-08-21</c> means that
    /// day in Bangkok regardless of where the caller's browser thinks it is.
    /// </summary>
    private DateTimeOffset ToClinicInstant(DateOnly date)
    {
        var midday = date.ToDateTime(new TimeOnly(12, 0));
        var offset = clock.ClinicTimeZone.GetUtcOffset(midday);

        return new DateTimeOffset(midday, offset);
    }
}
