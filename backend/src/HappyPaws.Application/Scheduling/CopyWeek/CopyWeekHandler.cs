using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling.CopyWeek;

/// <summary>
/// <c>POST /staff/schedules/shifts/copy-week</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 49</c>. Weeks are named by their Monday, the roster's own week.
/// </summary>
/// <param name="BranchId">Shifts at this branch are copied, and the copies are at this branch.</param>
/// <param name="FromWeek">The Monday of the week copied from.</param>
/// <param name="ToWeek">The Monday of the week copied to.</param>
/// <param name="StaffIds">Only these people; everyone with a shift there when null or empty.</param>
public sealed record CopyWeekCommand(
    string BranchId,
    DateOnly? FromWeek,
    DateOnly? ToWeek,
    IReadOnlyList<string>? StaffIds = null);

public sealed class CopyWeekValidator : AbstractValidator<CopyWeekCommand>
{
    public CopyWeekValidator()
    {
        RuleFor(command => command.BranchId).NotEmpty();

        RuleFor(command => command.FromWeek)
            .NotNull().WithMessage("ต้องระบุวันจันทร์ของสัปดาห์ต้นทาง")
            .Must(BeMonday).WithMessage("สัปดาห์ต้องระบุด้วยวันจันทร์ของสัปดาห์นั้น");

        RuleFor(command => command.ToWeek)
            .NotNull().WithMessage("ต้องระบุวันจันทร์ของสัปดาห์ปลายทาง")
            .Must(BeMonday).WithMessage("สัปดาห์ต้องระบุด้วยวันจันทร์ของสัปดาห์นั้น")
            .NotEqual(command => command.FromWeek).WithMessage("สัปดาห์ปลายทางต้องไม่ใช่สัปดาห์เดียวกับต้นทาง");

        RuleFor(command => command.StaffIds)
            .Must(ids => ids!.Count <= 100).WithMessage("ระบุพนักงานได้ครั้งละไม่เกิน 100 คน")
            .When(command => command.StaffIds is not null);
    }

    private static bool BeMonday(DateOnly? day) => day is null || day.Value.DayOfWeek == DayOfWeek.Monday;
}

/// <summary>
/// Copies one week of a branch's roster onto another week, at the same clinic times: Monday's
/// 09:00 to 18:00 becomes the next Monday's 09:00 to 18:00.
/// <para>
/// <b>What is copied.</b> Every active shift at the branch that starts in the source week, each
/// block on its own, so a shift with a break arrives as the same two blocks. With
/// <c>staff_ids</c>, only those people; each must be someone the caller may schedule, as on
/// create. Without it, people no longer working, or no longer at the branch, are left out rather
/// than refusing the whole week.
/// </para>
/// <para>
/// <b>All or nothing,</b> like create and with create's answer: if any copy would overlap a shift
/// the person already has (<c>409 shift_overlap</c>) or their time off (<c>409 staff_on_time_off</c>),
/// nothing is created and every clash is listed with whose it is. At most 200 copies a request,
/// the same ceiling as create; a larger branch copies in parts with <c>staff_ids</c>.
/// </para>
/// <para>
/// <b>No history.</b> A copy whose start has already passed is left out and counted in
/// <c>skipped_started</c>, so copying into the current week fills only the days still to come.
/// </para>
/// </summary>
public sealed class CopyWeekHandler(
    ScheduleAccess access,
    IScheduleRepository schedule,
    IStaffRepository staff,
    IBranchRepository branches,
    BranchGuard branchGuard,
    ClinicDays days,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<CopyWeekResultDto> HandleAsync(CopyWeekCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        branchGuard.EnsureCanAccess(command.BranchId);

        if ((await branches.MissingAsync([command.BranchId], cancellationToken)).Count > 0)
        {
            throw Invalid("branch_id", "ไม่พบสาขาที่เปิดอยู่");
        }

        var fromWeek = command.FromWeek!.Value;
        var toWeek = command.ToWeek!.Value;
        var shiftBy = toWeek.DayNumber - fromWeek.DayNumber;

        var week = (await schedule.ActiveShiftsAsync(
                days.StartOf(fromWeek), days.StartOf(fromWeek.AddDays(7)), [command.BranchId], null, cancellationToken))
            .Where(shift => shift.StartsAt >= days.StartOf(fromWeek))
            .ToList();
        var people = await PeopleAsync(command, week, cancellationToken);
        var source = week.Where(shift => people.Contains(shift.StaffId)).ToList();

        if (source.Count == 0)
        {
            throw Invalid("from_week", "สัปดาห์ต้นทางไม่มีเวรที่สาขานี้ให้คัดลอก");
        }

        var now = clock.UtcNow;
        var copies = source
            .Select(shift => new PlannedShift(shift.StaffId, Moved(shift.StartsAt, shiftBy), Moved(shift.EndsAt, shiftBy)))
            .ToList();
        var planned = copies.Where(copy => copy.StartsAt >= now).OrderBy(copy => copy.StartsAt).ToList();
        var skipped = copies.Count - planned.Count;

        if (planned.Count == 0)
        {
            throw Invalid("to_week", "เวรที่จะคัดลอกเริ่มไปแล้วทั้งหมด ไม่มีเวรให้คัดลอก");
        }

        if (planned.Count > ShiftRecurrence.MaxOccurrences)
        {
            throw Invalid(
                "staff_ids",
                $"คัดลอกได้ครั้งละไม่เกิน {ShiftRecurrence.MaxOccurrences} เวร เลือกบางคนแล้วคัดลอกทีละส่วน");
        }

        var created = new List<StaffShift>();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            created.Clear();

            var from = planned.Min(copy => copy.StartsAt);
            var to = planned.Max(copy => copy.EndsAt);

            ShiftClashes.EnsureNoOverlap(planned, await schedule.ActiveShiftsAsync(from, to, null, null, token));
            ShiftClashes.EnsureNotOnTimeOff(planned, await schedule.TimeOffAsync(from, to, null, token));

            foreach (var copy in planned)
            {
                var shift = StaffShift.Create(copy.StaffId, command.BranchId, copy.StartsAt, copy.EndsAt);
                schedule.AddShift(shift);
                created.Add(shift);
            }

            foreach (var person in created.GroupBy(shift => shift.StaffId))
            {
                audit.Record(new AuditEvent(
                    AuditActions.WeekCopied,
                    AuditResources.StaffMember,
                    person.Key,
                    BranchId: command.BranchId,
                    Details: new Dictionary<string, object?>
                    {
                        ["from_week"] = fromWeek.ToString("yyyy-MM-dd"),
                        ["to_week"] = toWeek.ToString("yyyy-MM-dd"),
                        ["shifts"] = person.Count(),
                    }));
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return new CopyWeekResultDto([.. created.Select(ShiftDto.From)], skipped);
    }

    /// <summary>
    /// Who may be copied. Named people must each be schedulable here, or the request is refused;
    /// otherwise anyone not, or no longer, schedulable here is quietly left out.
    /// </summary>
    private async Task<HashSet<string>> PeopleAsync(
        CopyWeekCommand command,
        IReadOnlyList<StaffShift> week,
        CancellationToken cancellationToken)
    {
        if (command.StaffIds is { Count: > 0 } named)
        {
            var ids = named.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).ToHashSet(StringComparer.Ordinal);

            foreach (var id in ids)
            {
                var member = await access.ActiveMemberAsync(id, cancellationToken);

                if (!WorksAt(member, command.BranchId))
                {
                    throw Invalid("staff_ids", $"พนักงาน {id} ไม่ได้ประจำสาขานี้");
                }
            }

            return ids;
        }

        var rows = await staff.FindRowsAsync([.. week.Select(shift => shift.StaffId).Distinct(StringComparer.Ordinal)], cancellationToken);

        return rows
            .Where(row => row.Membership.IsActive && access.IsVisible(row) && WorksAt(row, command.BranchId))
            .Select(row => row.Membership.Id)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static bool WorksAt(StaffMemberRow row, string branchId) =>
        row.Membership.BranchIds.Count == 0 || row.Membership.BranchIds.Contains(branchId);

    /// <summary>The same clinic wall-clock time, <paramref name="byDays"/> days later.</summary>
    private DateTimeOffset Moved(DateTimeOffset instant, int byDays)
    {
        var local = TimeZoneInfo.ConvertTime(instant, clock.ClinicTimeZone).DateTime.AddDays(byDays);

        return new DateTimeOffset(local, clock.ClinicTimeZone.GetUtcOffset(local));
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
