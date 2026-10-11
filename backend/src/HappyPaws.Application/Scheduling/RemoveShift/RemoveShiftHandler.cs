using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling.ChangeShift;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Application.Scheduling.RequestTimeOff;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling.RemoveShift;

/// <summary>
/// <c>DELETE /staff/schedules/shifts/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 47</c>.
/// </summary>
/// <param name="ShiftId">The block.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>; the check is skipped when absent.</param>
public sealed record RemoveShiftCommand(string ShiftId, uint? IfMatchVersion = null);

/// <summary>
/// Takes one block off the roster, so nothing new can be booked in it.
/// <para>
/// <b>One block, not one day.</b> A shift with a break is stored as two blocks; this removes the
/// one named. Taking the whole day off is two calls, or time off over the day.
/// </para>
/// <para>
/// <b>Cancelled, not deleted.</b> The row stays, marked cancelled, as time off leaves the shifts
/// it cuts, so the roster as planned is still on record. It drops out of every read, and a
/// second removal answers <c>404</c>.
/// </para>
/// <para>
/// <b>Only time still to come.</b> A block that has ended is history (<c>409 shift_ended</c>). A
/// block under way is refused too (<c>409 shift_started</c>): part of it has been worked, so the
/// way to cut it short is to change its end, or record time off from now.
/// </para>
/// <para>
/// <b>Booked appointments do not block it.</b> Each one inside the block is listed with who could
/// take it, as time off does. Nothing is moved.
/// </para>
/// </summary>
public sealed class RemoveShiftHandler(
    ScheduleAccess access,
    SubstituteFinder substitutes,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ShiftRemovedDto> HandleAsync(RemoveShiftCommand command, CancellationToken cancellationToken = default)
    {
        StaffShift? shift = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            shift = await access.ActiveShiftAsync(command.ShiftId, token);
            ConcurrencyGuard.Ensure(shift, command.IfMatchVersion);

            var now = clock.UtcNow;

            if (shift.HasEnded(now))
            {
                throw ChangeShiftHandler.Ended(shift.Id);
            }

            if (shift.HasStarted(now))
            {
                throw new ConflictException(ErrorCodes.ShiftStarted, "เวรนี้เริ่มไปแล้ว ลบไม่ได้ ให้แก้เวลาเลิกหรือบันทึกวันลาแทน")
                {
                    MessageEn = "This shift has begun. Change its end, or record time off, instead of removing it.",
                    Details = new Dictionary<string, object> { ["shift_id"] = shift.Id },
                };
            }

            shift.Cancel(now);

            audit.Record(new AuditEvent(
                AuditActions.ShiftRemoved,
                AuditResources.Shift,
                shift.Id,
                BranchId: shift.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["staff_id"] = shift.StaffId,
                    ["starts_at"] = shift.StartsAt,
                    ["ends_at"] = shift.EndsAt,
                }));

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        var conflicts = await ChangeShiftHandler.ConflictsAsync(
            substitutes, shift!.StaffId, [(shift.StartsAt, shift.EndsAt)], cancellationToken);

        return new ShiftRemovedDto(shift.Id, conflicts);
    }
}
