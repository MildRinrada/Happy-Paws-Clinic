using HappyPaws.Application.Integrations.ApiKeys;
using HappyPaws.Application.Integrations.GetCalendarFeed;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Integrations;

internal sealed class ApiKeyRepository(HappyPawsDbContext db) : IApiKeyRepository
{
    public void Add(ApiKey key) => db.Set<ApiKey>().Add(key);

    public Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        db.Set<ApiKey>().AsNoTracking().FirstOrDefaultAsync(key => key.Prefix == prefix, cancellationToken);

    public Task TouchAsync(string keyId, DateTimeOffset at, CancellationToken cancellationToken = default) =>
        db.Set<ApiKey>()
            .Where(key => key.Id == keyId)
            .ExecuteUpdateAsync(set => set.SetProperty(key => key.LastUsedAt, at), cancellationToken);
}

/// <summary>Calendar links, and the one read the feed makes: appointments joined to their names.</summary>
internal sealed class CalendarFeedRepository(HappyPawsDbContext db) : ICalendarFeedRepository
{
    public void Add(CalendarFeed feed) => db.Set<CalendarFeed>().Add(feed);

    public Task<CalendarFeed?> FindByStaffAsync(string staffId, CancellationToken cancellationToken = default) =>
        db.Set<CalendarFeed>().AsNoTracking().FirstOrDefaultAsync(feed => feed.StaffId == staffId, cancellationToken);

    public Task<CalendarFeed?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.Set<CalendarFeed>().AsNoTracking().FirstOrDefaultAsync(feed => feed.TokenHash == tokenHash, cancellationToken);

    public Task TouchAsync(string feedId, DateTimeOffset at, CancellationToken cancellationToken = default) =>
        db.Set<CalendarFeed>()
            .Where(feed => feed.Id == feedId)
            .ExecuteUpdateAsync(set => set.SetProperty(feed => feed.LastUsedAt, at), cancellationToken);

    public async Task<IReadOnlyList<CalendarEntry>> AppointmentsAsync(
        string? staffId,
        IReadOnlyCollection<string>? branchIds,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var appointments = db.Set<Appointment>().AsNoTracking()
            .Where(appointment => appointment.StartsAt < to && appointment.EndsAt > from
                && appointment.Status != AppointmentStatus.Draft);

        if (staffId is not null)
        {
            appointments = appointments.Where(appointment => appointment.StaffId == staffId);
        }

        if (branchIds is not null)
        {
            var visible = branchIds.ToArray();
            appointments = appointments.Where(appointment => visible.Contains(appointment.BranchId));
        }

        var rows = await (
            from appointment in appointments
            join service in db.Set<ClinicService>() on appointment.ServiceId equals service.Id
            join pet in db.Set<Pet>() on appointment.PetId equals pet.Id
            join branch in db.Set<Branch>() on appointment.BranchId equals branch.Id
            orderby appointment.StartsAt, appointment.Id
            select new
            {
                appointment.Id,
                appointment.StartsAt,
                appointment.EndsAt,
                appointment.Status,
                ServiceName = service.Name,
                PetName = pet.Name,
                BranchName = branch.Name,
                appointment.CreatedAt,
                appointment.UpdatedAt,
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new CalendarEntry(
            row.Id,
            row.StartsAt,
            row.EndsAt,
            row.Status,
            row.ServiceName,
            row.PetName,
            row.BranchName,
            row.UpdatedAt ?? row.CreatedAt))];
    }
}
