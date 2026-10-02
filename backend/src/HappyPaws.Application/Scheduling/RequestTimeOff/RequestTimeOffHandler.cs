using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling.RequestTimeOff;

/// <summary><c>POST /staff/schedules/time-off</c> — "ลา · ระบบเตือนนัดที่ชนและเสนอหมอแทน".</summary>
/// <param name="StaffId">Who is away.</param>
/// <param name="StartsAt">From.</param>
/// <param name="EndsAt">Until.</param>
/// <param name="Reason">Optional, e.g. "ลาป่วย".</param>
public sealed record RequestTimeOffCommand(
    string StaffId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Reason = null);

public sealed class RequestTimeOffValidator : AbstractValidator<RequestTimeOffCommand>
{
    public RequestTimeOffValidator()
    {
        RuleFor(command => command.StaffId).NotEmpty();
        RuleFor(command => command.EndsAt)
            .GreaterThan(command => command.StartsAt).WithMessage("เวลาสิ้นสุดการลาต้องอยู่หลังเวลาเริ่ม");
        RuleFor(command => command.Reason).MaximumLength(StaffTimeOff.ReasonMaxLength);
    }
}

/// <summary>
/// Records leave and takes it out of the roster.
/// <para>
/// <b>Recorded, not requested.</b> The contract names no approval step and no endpoint that would
/// approve one, so the person with <c>schedule:write</c> who records leave is the approval. It
/// takes effect at once: every shift it touches is cancelled and the parts still worked come back
/// as shifts of their own, so no new booking can land in it.
/// </para>
/// <para>
/// <b>Booked appointments stay where they are.</b> The answer lists each one inside the leave with
/// the colleagues who could take it ("เตือนนัดที่ชนและเสนอหมอแทน"). Moving one is an ordinary
/// reschedule, made by a person who can call the owner.
/// </para>
/// </summary>
public sealed class RequestTimeOffHandler(
    ScheduleAccess access,
    IScheduleRepository schedule,
    SubstituteFinder substitutes,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<TimeOffResultDto> HandleAsync(
        RequestTimeOffCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);
        await access.ActiveMemberAsync(command.StaffId, cancellationToken);

        StaffTimeOff? timeOff = null;
        var cancelled = 0;
        var remaining = new List<StaffShift>();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            remaining.Clear();

            var now = clock.UtcNow;
            timeOff = StaffTimeOff.Record(
                command.StaffId, command.StartsAt, command.EndsAt, command.Reason, currentUser.StaffId);

            var touched = await schedule.ActiveShiftsForStaffAsync(command.StaffId, command.StartsAt, command.EndsAt, token);

            foreach (var shift in touched)
            {
                foreach (var piece in shift.CarveOut(command.StartsAt, command.EndsAt, now))
                {
                    schedule.AddShift(piece);
                    remaining.Add(piece);
                }
            }

            cancelled = touched.Count;
            schedule.AddTimeOff(timeOff);

            audit.Record(new AuditEvent(
                AuditActions.TimeOffRecorded,
                AuditResources.TimeOff,
                timeOff.Id,
                Details: new Dictionary<string, object?>
                {
                    ["staff_id"] = command.StaffId,
                    ["starts_at"] = command.StartsAt,
                    ["ends_at"] = command.EndsAt,
                    ["shifts_cancelled"] = touched.Select(shift => shift.Id).ToArray(),
                }));

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        // Read after the commit: these are warnings about what is already booked, not part of it.
        var conflicts = await substitutes.ConflictsAsync(command.StaffId, command.StartsAt, command.EndsAt, cancellationToken);

        return new TimeOffResultDto(
            TimeOffDto.From(timeOff!),
            cancelled,
            [.. remaining.OrderBy(shift => shift.StartsAt).Select(ShiftDto.From)],
            conflicts);
    }
}
