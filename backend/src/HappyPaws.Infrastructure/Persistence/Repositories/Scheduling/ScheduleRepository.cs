using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Scheduling;

/// <summary>EF Core implementation of the roster port.</summary>
internal sealed class ScheduleRepository(HappyPawsDbContext db) : IScheduleRepository
{
    public void AddShift(StaffShift shift) => db.Set<StaffShift>().Add(shift);

    public void AddTimeOff(StaffTimeOff timeOff) => db.Set<StaffTimeOff>().Add(timeOff);

    public async Task<IReadOnlyList<StaffShift>> ActiveShiftsForStaffAsync(
        string staffId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await db.Set<StaffShift>()
            .Where(shift => shift.StaffId == staffId
                && shift.CancelledAt == null
                && shift.StartsAt < to
                && shift.EndsAt > from)
            .OrderBy(shift => shift.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StaffShift>> ActiveShiftsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyCollection<string>? branchIds,
        string? staffId,
        CancellationToken cancellationToken = default)
    {
        var shifts = db.Set<StaffShift>().AsNoTracking()
            .Where(shift => shift.CancelledAt == null && shift.StartsAt < to && shift.EndsAt > from);

        if (branchIds is not null)
        {
            var ids = branchIds.ToArray();
            shifts = shifts.Where(shift => ids.Contains(shift.BranchId));
        }

        if (staffId is { Length: > 0 })
        {
            shifts = shifts.Where(shift => shift.StaffId == staffId);
        }

        return await shifts.OrderBy(shift => shift.StartsAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StaffTimeOff>> TimeOffAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? staffId,
        CancellationToken cancellationToken = default)
    {
        var timeOff = db.Set<StaffTimeOff>().AsNoTracking()
            .Where(leave => leave.StartsAt < to && leave.EndsAt > from);

        if (staffId is { Length: > 0 })
        {
            timeOff = timeOff.Where(leave => leave.StaffId == staffId);
        }

        return await timeOff.OrderBy(leave => leave.StartsAt).ToListAsync(cancellationToken);
    }
}
