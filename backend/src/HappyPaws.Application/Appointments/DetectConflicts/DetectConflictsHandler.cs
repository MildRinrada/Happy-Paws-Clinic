using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Appointments.DetectConflicts;

/// <summary>
/// <c>GET /staff/appointments/conflicts</c> — "ตรวจนัดชน หมอลา ห้องไม่ว่าง ก่อนยืนยัน".
/// <para>
/// Two ways to ask. With <c>StartsAt</c> it checks a proposed visit before it is booked: the end
/// is <c>EndsAt</c>, or the start plus the service's length. With <c>Date</c> it checks the
/// visits already on that day, which is what to look at after someone's leave has been recorded.
/// </para>
/// </summary>
public sealed record DetectConflictsQuery(
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null,
    string? ServiceId = null,
    string? StaffId = null,
    string? PetId = null,
    string? BranchId = null,
    string? ExcludeAppointmentId = null,
    DateOnly? Date = null);

/// <summary>
/// Finds what would go wrong with a time, without booking anything.
/// <para>
/// Five kinds of problem, each with a reason a person can act on: the branch is shut that day, the
/// vet has no shift then, the vet is on leave, the vet already has someone, the pet already has a
/// visit. Nothing here refuses; it reports, so the counter can decide before pressing confirm.
/// </para>
/// <para>
/// <b>"ห้องไม่ว่าง" is not checked.</b> An appointment does not hold a room in this system; rooms
/// are given out on the queue board when the patient is called. There is no booked room to collide
/// with. See <c>docs/assumptions.md § 37</c>.
/// </para>
/// </summary>
public sealed class DetectConflictsHandler(
    IAppointmentRepository appointments,
    IScheduleRepository schedule,
    IBookingCatalog catalog,
    BranchGuard branchGuard,
    ClinicDays clinicDays)
{
    public async Task<ConflictReportDto> HandleAsync(
        DetectConflictsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.BranchId is { Length: > 0 } branchId)
        {
            branchGuard.EnsureCanAccess(branchId);
        }

        var conflicts = query switch
        {
            { StartsAt: { } startsAt } => await ProposalAsync(query, startsAt, cancellationToken),
            { Date: { } date } => await DayAsync(query, date, cancellationToken),
            _ => throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["starts_at"] = ["ส่ง starts_at เพื่อตรวจเวลาที่จะจอง หรือ date เพื่อตรวจนัดทั้งวัน"],
            }),
        };

        return new ConflictReportDto(conflicts.Count == 0, conflicts);
    }

    private async Task<List<AppointmentConflictDto>> ProposalAsync(
        DetectConflictsQuery query,
        DateTimeOffset startsAt,
        CancellationToken cancellationToken)
    {
        var endsAt = await EndOfAsync(query, startsAt, cancellationToken);
        var conflicts = new List<AppointmentConflictDto>();

        if (await ClosedBranchAsync(query.BranchId, startsAt, cancellationToken) is { } holiday)
        {
            conflicts.Add(new AppointmentConflictDto("clinic_closed", $"คลินิกปิด: {holiday}", startsAt, endsAt));
        }

        if (query.StaffId is { Length: > 0 } staffId)
        {
            var shifts = await schedule.ActiveShiftsAsync(startsAt.AddDays(-1), endsAt.AddDays(1), null, staffId, cancellationToken);

            if (!shifts.Any(shift => shift.Covers(startsAt, endsAt)))
            {
                conflicts.Add(new AppointmentConflictDto(
                    "staff_off_shift", "หมอไม่มีเวรในช่วงนี้", startsAt, endsAt, StaffId: staffId));
            }

            var leave = await schedule.TimeOffAsync(startsAt, endsAt, staffId, cancellationToken);
            conflicts.AddRange(leave.Select(off => new AppointmentConflictDto(
                "staff_on_time_off", LeaveMessage(off), startsAt, endsAt, StaffId: staffId)));

            var booked = await appointments.OccupyingInWindowAsync(startsAt, endsAt, null, staffId, null, cancellationToken);
            conflicts.AddRange(booked
                .Where(other => other.Id != query.ExcludeAppointmentId)
                .Select(other => new AppointmentConflictDto(
                    "staff_double_booked",
                    "หมอมีนัดอื่นในช่วงนี้",
                    startsAt,
                    endsAt,
                    StaffId: staffId,
                    ConflictingAppointmentId: other.Id)));
        }

        if (query.PetId is { Length: > 0 } petId)
        {
            var visits = await appointments.OccupyingForPetAsync(petId, startsAt, endsAt, query.ExcludeAppointmentId, cancellationToken);
            conflicts.AddRange(visits.Select(other => new AppointmentConflictDto(
                "pet_double_booked", "สัตว์ตัวนี้มีนัดอื่นในช่วงนี้", startsAt, endsAt, ConflictingAppointmentId: other.Id)));
        }

        return conflicts;
    }

    private async Task<List<AppointmentConflictDto>> DayAsync(
        DetectConflictsQuery query,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var from = clinicDays.StartOf(date);
        var to = clinicDays.StartOf(date.AddDays(1));

        var visits = await appointments.OccupyingInWindowAsync(
            from, to, query.BranchId, query.StaffId, branchGuard.VisibleBranchIds, cancellationToken);

        var shifts = await schedule.ActiveShiftsAsync(from.AddDays(-1), to.AddDays(1), null, query.StaffId, cancellationToken);
        var leave = await schedule.TimeOffAsync(from, to, query.StaffId, cancellationToken);

        var conflicts = new List<AppointmentConflictDto>();

        foreach (var branchId in visits.Select(visit => visit.BranchId).Distinct())
        {
            if (await catalog.HolidayAsync(branchId, date, cancellationToken) is { } holiday)
            {
                conflicts.AddRange(visits.Where(visit => visit.BranchId == branchId).Select(visit => Of(
                    visit, "clinic_closed", $"คลินิกปิด: {holiday.Name}")));
            }
        }

        foreach (var visit in visits.Where(visit => visit.StaffId is not null))
        {
            if (!shifts.Any(shift => shift.StaffId == visit.StaffId && shift.Covers(visit.StartsAt, visit.EndsAt)))
            {
                conflicts.Add(Of(visit, "staff_off_shift", "หมอไม่มีเวรในช่วงนัดนี้"));
            }

            conflicts.AddRange(leave
                .Where(off => off.StaffId == visit.StaffId && off.StartsAt < visit.EndsAt && visit.StartsAt < off.EndsAt)
                .Select(off => Of(visit, "staff_on_time_off", LeaveMessage(off))));
        }

        conflicts.AddRange(Overlaps(visits, visit => visit.StaffId, "staff_double_booked", "หมอมีสองนัดซ้อนกัน"));
        conflicts.AddRange(Overlaps(visits, visit => visit.PetId, "pet_double_booked", "สัตว์ตัวนี้มีสองนัดซ้อนกัน"));

        return conflicts;
    }

    /// <summary>Every pair of visits sharing a key and overlapping in time, reported against the later one.</summary>
    private static IEnumerable<AppointmentConflictDto> Overlaps(
        IReadOnlyList<Appointment> visits,
        Func<Appointment, string?> key,
        string kind,
        string message) =>
        visits
            .Where(visit => key(visit) is not null)
            .GroupBy(key)
            .SelectMany(group =>
            {
                var ordered = group.OrderBy(visit => visit.StartsAt).ThenBy(visit => visit.Id).ToList();

                return ordered.SelectMany((visit, index) => ordered
                    .Take(index)
                    .Where(earlier => earlier.EndsAt > visit.StartsAt)
                    .Select(earlier => Of(visit, kind, message) with { ConflictingAppointmentId = earlier.Id }));
            });

    private static AppointmentConflictDto Of(Appointment visit, string kind, string message) =>
        new(kind, message, visit.StartsAt, visit.EndsAt, visit.Id, visit.StaffId);

    private static string LeaveMessage(StaffTimeOff off) =>
        string.IsNullOrWhiteSpace(off.Reason) ? "หมอลาในช่วงนี้" : $"หมอลาในช่วงนี้: {off.Reason}";

    private async Task<DateTimeOffset> EndOfAsync(DetectConflictsQuery query, DateTimeOffset startsAt, CancellationToken cancellationToken)
    {
        if (query.EndsAt is { } endsAt)
        {
            return endsAt > startsAt
                ? endsAt
                : throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["ends_at"] = ["ends_at ต้องอยู่หลัง starts_at"],
                });
        }

        if (query.ServiceId is { Length: > 0 } serviceId)
        {
            var service = await catalog.FindServiceAsync(serviceId, cancellationToken)
                ?? throw new NotFoundException("service", serviceId);

            return startsAt.AddMinutes(service.DurationMinutes);
        }

        throw new ValidationFailedException(new Dictionary<string, string[]>
        {
            ["ends_at"] = ["ส่ง ends_at หรือ service_id เพื่อบอกว่านัดยาวเท่าไร"],
        });
    }

    private async Task<string?> ClosedBranchAsync(string? branchId, DateTimeOffset startsAt, CancellationToken cancellationToken)
    {
        var branch = branchId is { Length: > 0 } ? branchId : (await catalog.SoleActiveBranchAsync(cancellationToken))?.Id;

        return branch is null
            ? null
            : (await catalog.HolidayAsync(branch, clinicDays.DateOf(startsAt), cancellationToken))?.Name;
    }
}
