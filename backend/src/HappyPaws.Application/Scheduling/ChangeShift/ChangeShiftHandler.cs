using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Scheduling.CreateShifts;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Application.Scheduling.RequestTimeOff;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling.ChangeShift;

/// <summary>
/// <c>PATCH /staff/schedules/shifts/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 47</c>. Both times are required: the block's whole new window.
/// </summary>
/// <param name="ShiftId">The block, one of the two a shift with a break is stored as, or a whole shift.</param>
/// <param name="StartsAt">The new start.</param>
/// <param name="EndsAt">The new end.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>; the check is skipped when absent.</param>
public sealed record ChangeShiftCommand(
    string ShiftId,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    uint? IfMatchVersion = null);

/// <summary>The same day rules as creating a shift: the end after the start, at most 24 hours.</summary>
public sealed class ChangeShiftValidator : AbstractValidator<ChangeShiftCommand>
{
    public ChangeShiftValidator()
    {
        RuleFor(command => command.StartsAt).NotNull().WithMessage("ต้องระบุเวลาเริ่มเวร");
        RuleFor(command => command.EndsAt).NotNull().WithMessage("ต้องระบุเวลาเลิกเวร");

        RuleFor(command => command.EndsAt)
            .GreaterThan(command => command.StartsAt).WithMessage("เวลาสิ้นสุดเวรต้องอยู่หลังเวลาเริ่ม")
            .Must((command, endsAt) => endsAt - command.StartsAt <= CreateShiftsValidator.LongestShift)
            .WithMessage("เวรหนึ่งยาวได้ไม่เกิน 24 ชั่วโมง")
            .When(command => command.StartsAt is not null && command.EndsAt is not null);
    }
}

/// <summary>
/// Moves one block of the roster to new times.
/// <para>
/// <b>History stays put.</b> A block that has ended is refused with <c>409 shift_ended</c>. A block
/// under way keeps its start, since that time has been worked, and may only end later than now. A
/// block still to come may not be moved to start before now. Either way nothing done here rewrites
/// time that has passed.
/// </para>
/// <para>
/// <b>Same clash rules as creating.</b> The new window may not overlap the person's other shifts
/// at any branch (<c>409 shift_overlap</c>) or their time off (<c>409 staff_on_time_off</c>).
/// </para>
/// <para>
/// <b>Booked appointments do not block the change.</b> Whatever falls in the time the block no
/// longer covers is listed with who could take it, exactly as recording time off does. Nothing is
/// moved; a person calls the owner.
/// </para>
/// </summary>
public sealed class ChangeShiftHandler(
    ScheduleAccess access,
    IScheduleRepository schedule,
    SubstituteFinder substitutes,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<ShiftChangeResultDto> HandleAsync(ChangeShiftCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var startsAt = command.StartsAt!.Value;
        var endsAt = command.EndsAt!.Value;
        StaffShift? shift = null;
        var removed = new List<(DateTimeOffset From, DateTimeOffset To)>();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            removed.Clear();

            shift = await access.ActiveShiftAsync(command.ShiftId, token);
            ConcurrencyGuard.Ensure(shift, command.IfMatchVersion);

            // As on create: a deactivated member is no longer planned, so their shifts are not moved.
            await access.ActiveMemberAsync(shift.StaffId, token);

            EnsureLeavesHistoryAlone(shift, startsAt, endsAt, clock.UtcNow);

            if (shift.StartsAt == startsAt && shift.EndsAt == endsAt)
            {
                return;
            }

            var blocks = new[] { new PlannedShift(shift.StaffId, startsAt, endsAt) };
            var others = (await schedule.ActiveShiftsForStaffAsync(shift.StaffId, startsAt, endsAt, token))
                .Where(other => other.Id != shift.Id);

            ShiftClashes.EnsureNoOverlap(blocks, others);
            ShiftClashes.EnsureNotOnTimeOff(blocks, await schedule.TimeOffAsync(startsAt, endsAt, shift.StaffId, token));

            var before = (shift.StartsAt, shift.EndsAt);
            removed.AddRange(Uncovered(before.StartsAt, before.EndsAt, startsAt, endsAt));
            shift.Reschedule(startsAt, endsAt);

            audit.Record(new AuditEvent(
                AuditActions.ShiftChanged,
                AuditResources.Shift,
                shift.Id,
                BranchId: shift.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["staff_id"] = shift.StaffId,
                    ["starts_at_before"] = before.StartsAt,
                    ["ends_at_before"] = before.EndsAt,
                    ["starts_at"] = startsAt,
                    ["ends_at"] = endsAt,
                }));

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        // Read after the commit, like time off: warnings about what is already booked.
        var conflicts = await ConflictsAsync(substitutes, shift!.StaffId, removed, cancellationToken);

        return new ShiftChangeResultDto(ShiftDto.From(shift), conflicts);
    }

    /// <summary>
    /// The parts of the old window the new one no longer covers: none, one either side, or the
    /// whole old window when the two do not meet.
    /// </summary>
    public static IReadOnlyList<(DateTimeOffset From, DateTimeOffset To)> Uncovered(
        DateTimeOffset oldStart,
        DateTimeOffset oldEnd,
        DateTimeOffset newStart,
        DateTimeOffset newEnd)
    {
        if (newEnd <= oldStart || newStart >= oldEnd)
        {
            return [(oldStart, oldEnd)];
        }

        var parts = new List<(DateTimeOffset, DateTimeOffset)>();

        if (oldStart < newStart)
        {
            parts.Add((oldStart, newStart));
        }

        if (newEnd < oldEnd)
        {
            parts.Add((newEnd, oldEnd));
        }

        return parts;
    }

    /// <summary>Booked appointments in any of the windows, each once, earliest first.</summary>
    internal static async Task<IReadOnlyList<TimeOffConflictDto>> ConflictsAsync(
        SubstituteFinder substitutes,
        string staffId,
        IEnumerable<(DateTimeOffset From, DateTimeOffset To)> windows,
        CancellationToken cancellationToken)
    {
        var found = new List<TimeOffConflictDto>();

        foreach (var (from, to) in windows)
        {
            found.AddRange(await substitutes.ConflictsAsync(staffId, from, to, cancellationToken));
        }

        return [.. found.DistinctBy(conflict => conflict.AppointmentId).OrderBy(conflict => conflict.StartsAt)];
    }

    private static void EnsureLeavesHistoryAlone(StaffShift shift, DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset now)
    {
        if (shift.HasEnded(now))
        {
            throw Ended(shift.Id);
        }

        if (shift.HasStarted(now) && startsAt != shift.StartsAt)
        {
            throw Invalid("starts_at", "เวรนี้เริ่มไปแล้ว เปลี่ยนเวลาเริ่มไม่ได้");
        }

        if (!shift.HasStarted(now) && startsAt < now)
        {
            throw Invalid("starts_at", "เวลาเริ่มต้องไม่อยู่ก่อนเวลานี้");
        }

        if (endsAt <= now)
        {
            throw Invalid("ends_at", "เวลาเลิกต้องอยู่หลังเวลานี้");
        }
    }

    /// <summary><c>409 shift_ended</c>, shared with removing a shift.</summary>
    internal static ConflictException Ended(string shiftId) =>
        new(ErrorCodes.ShiftEnded, "เวรนี้ผ่านไปแล้ว เป็นประวัติ แก้หรือลบไม่ได้")
        {
            MessageEn = "This shift is over; it is history and cannot be changed or removed.",
            Details = new Dictionary<string, object> { ["shift_id"] = shiftId },
        };

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
