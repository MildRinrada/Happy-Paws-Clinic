using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling.CreateShifts;

/// <summary><c>POST /staff/schedules/shifts</c> — "สร้างเวรเดี่ยวหรือทั้งสัปดาห์ด้วย RRULE".</summary>
/// <param name="StaffId">Whose shift.</param>
/// <param name="BranchId">Where.</param>
/// <param name="StartsAt">The first shift's start.</param>
/// <param name="EndsAt">The first shift's end.</param>
/// <param name="BreakStartsAt">Start of a break inside the first shift, or null for none.</param>
/// <param name="BreakEndsAt">End of that break.</param>
/// <param name="Rrule">An RRULE repeating the first shift, or null for one shift only.</param>
public sealed record CreateShiftsCommand(
    string StaffId,
    string BranchId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? BreakStartsAt = null,
    DateTimeOffset? BreakEndsAt = null,
    string? Rrule = null);

public sealed class CreateShiftsValidator : AbstractValidator<CreateShiftsCommand>
{
    /// <summary>A shift is a working day, not a week; longer is almost certainly a typo in the date.</summary>
    public static readonly TimeSpan LongestShift = TimeSpan.FromHours(24);

    public CreateShiftsValidator()
    {
        RuleFor(command => command.StaffId).NotEmpty();
        RuleFor(command => command.BranchId).NotEmpty();

        RuleFor(command => command.EndsAt)
            .GreaterThan(command => command.StartsAt).WithMessage("เวลาสิ้นสุดเวรต้องอยู่หลังเวลาเริ่ม")
            .Must((command, endsAt) => endsAt - command.StartsAt <= LongestShift)
            .WithMessage("เวรหนึ่งยาวได้ไม่เกิน 24 ชั่วโมง");

        RuleFor(command => command.BreakEndsAt)
            .NotNull().WithMessage("ต้องระบุทั้งเวลาเริ่มและเวลาจบของเวลาพัก")
            .When(command => command.BreakStartsAt is not null);

        RuleFor(command => command.BreakStartsAt)
            .NotNull().WithMessage("ต้องระบุทั้งเวลาเริ่มและเวลาจบของเวลาพัก")
            .When(command => command.BreakEndsAt is not null);

        RuleFor(command => command)
            .Must(command => command.BreakStartsAt > command.StartsAt
                && command.BreakEndsAt < command.EndsAt
                && command.BreakEndsAt > command.BreakStartsAt)
            .WithName("break")
            .WithMessage("เวลาพักต้องอยู่ภายในเวร และไม่ชิดต้นหรือท้ายเวร")
            .When(command => command.BreakStartsAt is not null && command.BreakEndsAt is not null);

        RuleFor(command => command.Rrule).MaximumLength(300);
    }
}

/// <summary>
/// Puts shifts on the roster, and with that, makes time bookable: "ตารางเวรเป็นตัวกำหนด slot ที่
/// /booking/availability คำนวณ".
/// <para>
/// <b>A break is a gap, not a flag.</b> A shift with a break becomes two shifts, before and after
/// it. Booking already offers only time inside active shifts, so a break needs no rule of its own
/// there, and the calendar shows it as what it is: time the person is not taking cases.
/// </para>
/// <para>
/// <b>All or nothing.</b> If any shift the request would create overlaps a shift the person
/// already has, or falls in their recorded time off, nothing is created and the answer lists every
/// clash. A half-created week is harder to fix than a refused one. The same check makes a retried
/// request safe: the second attempt overlaps the first and is refused.
/// </para>
/// </summary>
public sealed class CreateShiftsHandler(
    ScheduleAccess access,
    IScheduleRepository schedule,
    IBranchRepository branches,
    BranchGuard branchGuard,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<IReadOnlyList<ShiftDto>> HandleAsync(
        CreateShiftsCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        branchGuard.EnsureCanAccess(command.BranchId);

        var member = await access.ActiveMemberAsync(command.StaffId, cancellationToken);

        if ((await branches.MissingAsync([command.BranchId], cancellationToken)).Count > 0)
        {
            throw Invalid("branch_id", "ไม่พบสาขาที่เปิดอยู่");
        }

        if (member.Membership.BranchIds.Count > 0 && !member.Membership.BranchIds.Contains(command.BranchId))
        {
            throw Invalid("branch_id", "พนักงานคนนี้ไม่ได้ประจำสาขานี้");
        }

        var planned = Plan(command);
        var created = new List<StaffShift>();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            created.Clear();

            var from = planned.Min(shift => shift.StartsAt);
            var to = planned.Max(shift => shift.EndsAt);

            var blocks = planned.Select(block => new PlannedShift(command.StaffId, block.StartsAt, block.EndsAt)).ToList();

            ShiftClashes.EnsureNoOverlap(blocks, await schedule.ActiveShiftsForStaffAsync(command.StaffId, from, to, token));
            ShiftClashes.EnsureNotOnTimeOff(blocks, await schedule.TimeOffAsync(from, to, command.StaffId, token));

            foreach (var (startsAt, endsAt) in planned)
            {
                var shift = StaffShift.Create(command.StaffId, command.BranchId, startsAt, endsAt);
                schedule.AddShift(shift);
                created.Add(shift);
            }

            audit.Record(new AuditEvent(
                AuditActions.ShiftsCreated,
                AuditResources.StaffMember,
                command.StaffId,
                BranchId: command.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["shifts"] = created.Count,
                    ["first_starts_at"] = from,
                    ["last_ends_at"] = to,
                    ["rrule"] = command.Rrule,
                }));

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return [.. created.Select(ShiftDto.From)];
    }

    /// <summary>Every block the request asks for: each occurrence, split around its break.</summary>
    private List<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> Plan(CreateShiftsCommand command)
    {
        var length = command.EndsAt - command.StartsAt;
        var blocks = new List<(DateTimeOffset, DateTimeOffset)>();
        IReadOnlyList<DateTimeOffset> starts;

        try
        {
            starts = ShiftRecurrence.Expand(command.StartsAt, command.Rrule, clock.ClinicTimeZone);
        }
        catch (DomainException refused)
        {
            // A rule that cannot be read is a mistake in the request, not a clash with the roster.
            throw Invalid("rrule", refused.Message);
        }

        foreach (var start in starts)
        {
            var end = start + length;

            if (command.BreakStartsAt is { } breakStart && command.BreakEndsAt is { } breakEnd)
            {
                blocks.Add((start, start + (breakStart - command.StartsAt)));
                blocks.Add((start + (breakEnd - command.StartsAt), end));
            }
            else
            {
                blocks.Add((start, end));
            }
        }

        return blocks;
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
