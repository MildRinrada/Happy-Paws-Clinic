using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling;

/// <summary>Persistence port for the roster: shifts and time off.</summary>
public interface IScheduleRepository
{
    void AddShift(StaffShift shift);

    void AddTimeOff(StaffTimeOff timeOff);

    /// <summary>
    /// One person's active shifts overlapping a window, at any branch, tracked: time off cancels
    /// them, and a new shift is checked against them.
    /// </summary>
    Task<IReadOnlyList<StaffShift>> ActiveShiftsForStaffAsync(
        string staffId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>Active shifts overlapping a window, untracked, earliest first, for reading the roster.</summary>
    /// <param name="from">Window start.</param>
    /// <param name="to">Window end.</param>
    /// <param name="branchIds">Only these branches; null for all.</param>
    /// <param name="staffId">Only this person; null for everyone.</param>
    /// <param name="cancellationToken">Aborts the query.</param>
    Task<IReadOnlyList<StaffShift>> ActiveShiftsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyCollection<string>? branchIds,
        string? staffId,
        CancellationToken cancellationToken = default);

    /// <summary>Time off overlapping a window, earliest first, optionally for one person.</summary>
    Task<IReadOnlyList<StaffTimeOff>> TimeOffAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? staffId,
        CancellationToken cancellationToken = default);
}
