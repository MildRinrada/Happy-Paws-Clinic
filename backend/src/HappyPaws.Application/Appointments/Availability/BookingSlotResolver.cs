using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.Appointments.Availability;

/// <summary>
/// What the caller is asking to occupy.
/// <para>
/// <c>BypassLeadTime</c> is true for staff. The contract is explicit that
/// <c>POST /staff/appointments</c> "ข้าม lead time ได้" — a counter cannot refuse a pet that is
/// already standing there because the booking page would have wanted two hours' notice.
/// </para>
/// <para>
/// <c>RequireOnlineBookable</c> is true for the customer-facing paths, which may only book
/// services the clinic has opened for online booking. The two exclusions carry the record being
/// moved, so rescheduling does not collide with the slot it is vacating and redeeming a hold is
/// not blocked by that same hold.
/// </para>
/// </summary>
/// <param name="ServiceId">The service being booked.</param>
/// <param name="StaffId">The vet asked for, or null to have one chosen.</param>
/// <param name="BranchId">The branch, or null in a one-branch clinic.</param>
/// <param name="StartsAt">The requested start.</param>
/// <param name="BypassLeadTime">True for staff.</param>
/// <param name="RequireOnlineBookable">True for the customer paths.</param>
/// <param name="ExcludeAppointmentId">The appointment being moved, if any.</param>
/// <param name="ExcludeHoldId">The hold being redeemed, if any.</param>
/// <param name="Species">
/// The pet's species when it is known, so the clinic's assignment rules can prefer, say, the cat
/// vet for a cat. Only used when no vet is named.
/// </param>
/// <param name="DurationMinutes">
/// How long the visit takes when the appointment type says so, overriding the service. Null to
/// use the service's own length.
/// </param>
/// <param name="LeadTimeMinutes">
/// The notice a customer must give for this appointment type, overriding the clinic's default.
/// Ignored when <paramref name="BypassLeadTime"/> is true.
/// </param>
/// <param name="BypassAdvanceLimit">
/// True for a course of visits booked by staff. The clinic's "จองล่วงหน้าได้กี่วัน" is a limit on
/// the booking page; a six-session course every three weeks cannot fit inside sixty days.
/// </param>
public sealed record SlotRequest(
    string ServiceId,
    string? StaffId,
    string? BranchId,
    DateTimeOffset StartsAt,
    bool BypassLeadTime,
    bool RequireOnlineBookable,
    string? ExcludeAppointmentId = null,
    string? ExcludeHoldId = null,
    string? Species = null,
    int? DurationMinutes = null,
    int? LeadTimeMinutes = null,
    bool BypassAdvanceLimit = false);

/// <summary>A slot that has been checked and may be written.</summary>
public sealed record ResolvedSlot(
    Branch Branch,
    ClinicService Service,
    string StaffId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

/// <summary>
/// Validates one requested appointment window against the roster, the calendar and the clinic's
/// booking rules — and refuses with <c>slot_unavailable</c> plus real alternatives.
/// <para>
/// Shared by customer booking, staff booking and rescheduling. Those three differ only in who may
/// bypass the lead time and whose ownership is checked; the slot arithmetic is identical, and
/// three copies of it would drift the first time a rule changed.
/// </para>
/// <para>
/// This resolves and refuses. It does not write, and it does not commit — the caller re-runs the
/// check inside its transaction, because a resolution made before a transaction opens is only a
/// prediction. See <c>docs/architecture.md</c> on the double check.
/// </para>
/// </summary>
public sealed class BookingSlotResolver(
    IBookingCatalog catalog,
    IAppointmentRepository appointments,
    ISlotHoldRepository holds,
    IClock clock)
{
    public async Task<ResolvedSlot> ResolveAsync(SlotRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await catalog.GetBookingSettingsAsync(cancellationToken);
        var service = await ServiceOrThrowAsync(request, cancellationToken);
        var branch = await BranchOrThrowAsync(request.BranchId, cancellationToken);

        if (!service.IsOfferedAt(branch.Id))
        {
            // "จำกัดสาขา" binds everyone, staff included: the service does not exist at this site.
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["service_id"] = ["บริการนี้ไม่เปิดให้บริการที่สาขานี้"] });
        }

        var startsAt = request.StartsAt;
        var duration = request.DurationMinutes ?? service.DurationMinutes;
        var endsAt = startsAt.AddMinutes(duration);

        if (await catalog.HolidayAsync(branch.Id, ClinicDate(startsAt), cancellationToken) is { } holiday)
        {
            throw SlotUnavailableException.ClinicClosed(holiday.Name);
        }

        var (dayFrom, dayTo) = ClinicDayBounds(startsAt);
        var shifts = await catalog.ShiftsAsync(branch.Id, dayFrom, dayTo, request.StaffId, cancellationToken);
        var staffIds = shifts.Select(shift => shift.StaffId).Distinct(StringComparer.Ordinal).ToArray();

        var occupied = await appointments.OccupyingSlotsAsync(
            branch.Id, staffIds, dayFrom, dayTo, request.ExcludeAppointmentId, cancellationToken);

        var liveHolds = await holds.ActiveInWindowAsync(
            branch.Id, staffIds, dayFrom, dayTo, clock.UtcNow, request.ExcludeHoldId, cancellationToken);

        var query = new SlotQuery(
            shifts,
            occupied,
            liveHolds,
            settings.SlotMinutes,
            duration,
            EarliestStart(settings, request.BypassLeadTime, request.LeadTimeMinutes),
            request.BypassAdvanceLimit ? DateTimeOffset.MaxValue : clock.UtcNow.AddDays(settings.MaxAdvanceDays),
            service.EligibleStaffIds,

            // The service's online windows bind the customer paths only, like the online switch
            // itself. Set on the query so the alternatives offered with a refusal respect them too.
            request.RequireOnlineBookable ? start => service.IsBookableOnlineAt(ClinicLocal(start)) : null);

        // Suggestions are for the day the caller asked about. A shift that runs past midnight
        // would otherwise let a refusal for Monday afternoon answer "try 01:30" — technically an
        // opening, and not an answer anyone wants.
        var sameDay = query with
        {
            EarliestStart = Later(query.EarliestStart, dayFrom),
            LatestStart = Earlier(query.LatestStart, dayTo.AddTicks(-1)),
        };

        EnsureWithinBookingWindow(settings, request, startsAt, query, sameDay);

        if (request.RequireOnlineBookable && !service.IsBookableOnlineAt(ClinicLocal(startsAt)))
        {
            throw SlotUnavailableException.OutsideBookingWindow(
                "บริการนี้เปิดให้จองออนไลน์เฉพาะบางช่วงเวลา กรุณาเลือกเวลาอื่นหรือติดต่อคลินิก",
                "This service can only be booked online at certain times.",
                Suggest(sameDay));
        }

        var staffId = request.StaffId
            ?? PickStaff(sameDay, service, startsAt, endsAt, shifts, await catalog.AssignmentRulesAsync(cancellationToken), request.Species);

        if (!service.CanBePerformedBy(staffId))
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["staff_id"] = ["ผู้ให้บริการนี้ไม่สามารถให้บริการที่เลือกได้"] });
        }

        if (!SlotCalculator.IsCoveredByShift(shifts, staffId, startsAt, endsAt))
        {
            throw SlotUnavailableException.OutsideShift(Suggest(sameDay));
        }

        if (!SlotCalculator.IsFree(occupied, liveHolds, staffId, startsAt, endsAt))
        {
            throw SlotUnavailableException.AlreadyBooked(Suggest(sameDay));
        }

        return new ResolvedSlot(branch, service, staffId, startsAt, endsAt);
    }

    private static DateTimeOffset Later(DateTimeOffset first, DateTimeOffset second) =>
        first > second ? first : second;

    private static DateTimeOffset Earlier(DateTimeOffset first, DateTimeOffset second) =>
        first < second ? first : second;

    /// <summary>The window a whole clinic day spans, used to gather shifts and appointments once.</summary>
    public (DateTimeOffset From, DateTimeOffset To) ClinicDayBounds(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, clock.ClinicTimeZone);
        var startOfDay = new DateTimeOffset(local.Year, local.Month, local.Day, 0, 0, 0, local.Offset);

        return (startOfDay, startOfDay.AddDays(1));
    }

    /// <summary>The clinic-local calendar day an instant falls on — what a holiday is declared against.</summary>
    public DateOnly ClinicDate(DateTimeOffset instant) => DateOnly.FromDateTime(ClinicLocal(instant));

    /// <summary>An instant as the wall clock in the clinic reads it.</summary>
    public DateTime ClinicLocal(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, clock.ClinicTimeZone).DateTime;

    private DateTimeOffset EarliestStart(BookingSettings settings, bool bypassLeadTime, int? leadTimeMinutes) =>
        bypassLeadTime
            // Staff may book into the past by minutes when they are catching up on a walk-in that
            // already started; they may not backdate a visit into yesterday.
            ? clock.UtcNow.AddHours(-1)
            : clock.UtcNow.AddMinutes(leadTimeMinutes ?? settings.LeadTimeMinutes);

    /// <summary>
    /// Checks against the unclamped window (lead time, how far ahead the clinic books) but offers
    /// alternatives from the requested day.
    /// </summary>
    private void EnsureWithinBookingWindow(
        BookingSettings settings,
        SlotRequest request,
        DateTimeOffset startsAt,
        SlotQuery query,
        SlotQuery sameDay)
    {
        if (startsAt < query.EarliestStart)
        {
            var leadTime = request.LeadTimeMinutes ?? settings.LeadTimeMinutes;

            throw SlotUnavailableException.OutsideBookingWindow(
                request.BypassLeadTime
                    ? "จองย้อนหลังไม่ได้"
                    : $"ต้องจองล่วงหน้าอย่างน้อย {leadTime} นาที",
                request.BypassLeadTime
                    ? "Appointments cannot be booked in the past."
                    : $"Bookings require at least {leadTime} minutes' notice.",
                Suggest(sameDay));
        }

        if (startsAt > query.LatestStart)
        {
            throw SlotUnavailableException.OutsideBookingWindow(
                $"จองล่วงหน้าได้ไม่เกิน {settings.MaxAdvanceDays} วัน",
                $"Bookings are accepted up to {settings.MaxAdvanceDays} days ahead.",
                []);
        }
    }

    /// <summary>
    /// Chooses a vet when the caller did not name one — someone eligible and actually free for that
    /// exact window. The clinic's assignment rules put the people they prefer first ("หมอแมวรับเคส
    /// แมวก่อน"); everyone else follows in a stable order, so the same request twice picks the same
    /// person, and a rule never makes a free slot unbookable.
    /// </summary>
    private string PickStaff(
        SlotQuery query,
        ClinicService service,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        IReadOnlyList<Domain.Scheduling.StaffShift> shifts,
        Domain.Scheduling.AssignmentRules rules,
        string? species)
    {
        var free = shifts
            .Where(shift => shift.Covers(startsAt, endsAt) && service.CanBePerformedBy(shift.StaffId))
            .Select(shift => shift.StaffId)
            .Where(staffId => SlotCalculator.IsFree(query.Occupied, query.Holds, staffId, startsAt, endsAt));

        var candidate = rules.Rank(free, service.Id, species).FirstOrDefault();

        return candidate ?? throw SlotUnavailableException.AlreadyBooked(Suggest(query));
    }

    /// <summary>
    /// The alternatives that ride along with a refusal. Formatted in clinic-local time because a
    /// customer reads "11:00" as the time they would arrive, not as a UTC instant.
    /// </summary>
    private IReadOnlyList<string> Suggest(SlotQuery query) =>
        SlotCalculator.SuggestTimes(SlotCalculator.Enumerate(query), clock.ClinicTimeZone);

    private async Task<ClinicService> ServiceOrThrowAsync(SlotRequest request, CancellationToken cancellationToken)
    {
        var service = await catalog.FindServiceAsync(request.ServiceId, cancellationToken)
            ?? throw new NotFoundException("service", request.ServiceId);

        if (!service.IsActive)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["service_id"] = ["บริการนี้ปิดให้บริการแล้ว"] });
        }

        if (request.RequireOnlineBookable && !service.OnlineBookingEnabled)
        {
            // Not a 403: the service is real and public, it simply is not self-service. Telling the
            // customer to call is more useful than telling them they lack permission.
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["service_id"] = ["บริการนี้ไม่เปิดให้จองออนไลน์ กรุณาติดต่อคลินิก"] });
        }

        return service;
    }

    private async Task<Branch> BranchOrThrowAsync(string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is { Length: > 0 })
        {
            var branch = await catalog.FindBranchAsync(branchId, cancellationToken)
                ?? throw new NotFoundException("branch", branchId);

            return branch.IsActive
                ? branch
                : throw new ValidationFailedException(
                    new Dictionary<string, string[]> { ["branch_id"] = ["สาขานี้ปิดให้บริการแล้ว"] });
        }

        return await catalog.SoleActiveBranchAsync(cancellationToken)
            ?? throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["branch_id"] = ["คลินิกมีหลายสาขา ต้องระบุสาขา"] });
    }
}
