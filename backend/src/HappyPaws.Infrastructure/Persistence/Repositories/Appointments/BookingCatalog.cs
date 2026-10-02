using HappyPaws.Application.Appointments;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Scheduling;
using HappyPaws.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Appointments;

/// <summary>
/// EF Core implementation of the booking read port: clinic rules, services, branches and shifts.
/// </summary>
internal sealed class BookingCatalog(HappyPawsDbContext db) : IBookingCatalog
{
    public async Task<BookingSettings> GetBookingSettingsAsync(CancellationToken cancellationToken = default) =>
        await db.Set<BookingSettings>()
            .AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.Id == BookingSettings.SingletonId, cancellationToken)
        // Seeded by migration, so this should not happen. Falling back to the documented defaults
        // rather than throwing keeps a clinic bookable through a botched restore; the settings
        // endpoint will write a real row the first time anyone opens it.
        ?? BookingSettings.CreateDefault();

    public Task<ClinicService?> FindServiceAsync(string serviceId, CancellationToken cancellationToken = default) =>
        db.Set<ClinicService>().AsNoTracking()
            .FirstOrDefaultAsync(service => service.Id == serviceId, cancellationToken);

    public Task<Branch?> FindBranchAsync(string branchId, CancellationToken cancellationToken = default) =>
        db.Set<Branch>().AsNoTracking()
            .FirstOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public async Task<IReadOnlyList<StaffShift>> ShiftsAsync(
        string branchId,
        DateTimeOffset from,
        DateTimeOffset to,
        string? staffId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<StaffShift>()
            .AsNoTracking()
            .Where(shift => shift.BranchId == branchId
                && shift.CancelledAt == null
                && shift.StartsAt < to
                && shift.EndsAt > from);

        if (staffId is { Length: > 0 })
        {
            query = query.Where(shift => shift.StaffId == staffId);
        }

        return await query.OrderBy(shift => shift.StartsAt).ToListAsync(cancellationToken);
    }

    public Task<ClinicHoliday?> HolidayAsync(
        string branchId,
        DateOnly date,
        CancellationToken cancellationToken = default) =>
        db.Set<ClinicHoliday>().AsNoTracking()
            .Where(holiday => holiday.Date == date
                && (holiday.BranchId == null || holiday.BranchId == branchId))

            // A clinic-wide closure is reported ahead of a branch one, so the refusal names the
            // reason that applies to everybody.
            .OrderBy(holiday => holiday.BranchId == null ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Branch?> SoleActiveBranchAsync(CancellationToken cancellationToken = default)
    {
        // Two rows are fetched so "exactly one" can be distinguished from "several" without a
        // separate count query. A multi-branch clinic gets null and the caller asks for a branch.
        var branches = await db.Set<Branch>()
            .AsNoTracking()
            .Where(branch => branch.IsActive)
            .OrderBy(branch => branch.Code)
            .Take(2)
            .ToListAsync(cancellationToken);

        return branches.Count == 1 ? branches[0] : null;
    }

    public async Task<AssignmentRules> AssignmentRulesAsync(CancellationToken cancellationToken = default) =>
        await db.Set<AssignmentRules>().AsNoTracking()
            .FirstOrDefaultAsync(rules => rules.Id == AssignmentRules.SingletonId, cancellationToken)
        ?? AssignmentRules.CreateDefault();
}
