using HappyPaws.Application.Clinics;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Queueing;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Clinics;

/// <summary>
/// EF Core implementation of the one-row settings port.
/// <para>
/// A settings row that has never been written is answered with the type's own default rather than
/// a null every caller would have to handle. Only <see cref="LoadForUpdateAsync"/> ever adds that
/// default to the context, so a read can never create a row as a side effect.
/// </para>
/// </summary>
internal sealed class ClinicSettingsStore(HappyPawsDbContext db) : IClinicSettingsStore
{
    public async Task<TSettings> ReadAsync<TSettings>(
        string id,
        Func<TSettings> createDefault,
        CancellationToken cancellationToken = default)
        where TSettings : Entity =>
        await db.Set<TSettings>().AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.Id == id, cancellationToken)
        ?? createDefault();

    public async Task<TSettings> LoadForUpdateAsync<TSettings>(
        string id,
        Func<TSettings> createDefault,
        CancellationToken cancellationToken = default)
        where TSettings : Entity
    {
        var existing = await db.Set<TSettings>()
            .FirstOrDefaultAsync(settings => settings.Id == id, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = createDefault();
        db.Set<TSettings>().Add(created);

        return created;
    }
}

/// <summary>EF Core implementation of the branch port.</summary>
internal sealed class BranchRepository(HappyPawsDbContext db) : IBranchRepository
{
    public void Add(Branch branch) => db.Set<Branch>().Add(branch);

    public async Task<IReadOnlyList<Branch>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await db.Set<Branch>().AsNoTracking()
            .Where(branch => branch.IsActive)
            .OrderBy(branch => branch.Code)
            .ToListAsync(cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        db.Set<Branch>().AnyAsync(branch => branch.Code == code, cancellationToken);

    public Task<Branch?> FindOpenTrackedAsync(string branchId, CancellationToken cancellationToken = default) =>
        db.Set<Branch>().FirstOrDefaultAsync(branch => branch.Id == branchId && branch.IsActive, cancellationToken);

    public async Task<IReadOnlyList<ExamRoomUse>> ExamRoomUsesAsync(
        string branchId,
        IReadOnlyCollection<string> roomIds,
        DateOnly from,
        CancellationToken cancellationToken = default)
    {
        var rooms = roomIds.ToArray();
        var live = QueueTicketTransitions.LiveStatuses;

        // "Still on a queue" is the board's own definition, so a room is held exactly as long as
        // the waiting room can still be sent to it.
        return await db.Set<QueueTicket>().AsNoTracking()
            .Where(ticket => ticket.BranchId == branchId
                && ticket.ServiceDate >= from
                && ticket.RoomId != null
                && rooms.Contains(ticket.RoomId)
                && live.Contains(ticket.Status))
            .OrderBy(ticket => ticket.ServiceDate).ThenBy(ticket => ticket.Sequence)
            .Select(ticket => new ExamRoomUse(ticket.Id, ticket.Number, ticket.RoomId!, ticket.ServiceDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> MissingAsync(
        IReadOnlyCollection<string> branchIds,
        CancellationToken cancellationToken = default)
    {
        var ids = branchIds.ToArray();

        var found = await db.Set<Branch>().AsNoTracking()
            .Where(branch => ids.Contains(branch.Id) && branch.IsActive)
            .Select(branch => branch.Id)
            .ToListAsync(cancellationToken);

        return [.. ids.Except(found, StringComparer.Ordinal)];
    }
}

/// <summary>EF Core implementation of the contact-entry port.</summary>
internal sealed class ContactRepository(HappyPawsDbContext db) : IContactRepository
{
    public void Add(ClinicContact contact) => db.Set<ClinicContact>().Add(contact);

    public void Remove(ClinicContact contact) => db.Set<ClinicContact>().Remove(contact);

    public Task<ClinicContact?> FindTrackedAsync(string contactId, CancellationToken cancellationToken = default) =>
        db.Set<ClinicContact>().FirstOrDefaultAsync(contact => contact.Id == contactId, cancellationToken);

    public async Task<IReadOnlyList<ClinicContact>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<ClinicContact>().AsNoTracking()
            .OrderBy(contact => contact.SortOrder).ThenBy(contact => contact.CreatedAt).ThenBy(contact => contact.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        db.Set<ClinicContact>().AnyAsync(cancellationToken);
}

/// <summary>EF Core implementation of the holiday port.</summary>
internal sealed class HolidayRepository(HappyPawsDbContext db) : IHolidayRepository
{
    public void Add(ClinicHoliday holiday) => db.Set<ClinicHoliday>().Add(holiday);

    public Task<bool> ExistsAsync(DateOnly date, string? branchId, CancellationToken cancellationToken = default) =>
        db.Set<ClinicHoliday>()
            .AnyAsync(holiday => holiday.Date == date && holiday.BranchId == branchId, cancellationToken);

    public async Task<IReadOnlyList<ClinicHoliday>> FromAsync(
        DateOnly from,
        int take,
        CancellationToken cancellationToken = default) =>
        await db.Set<ClinicHoliday>().AsNoTracking()
            .Where(holiday => holiday.Date >= from)
            .OrderBy(holiday => holiday.Date).ThenBy(holiday => holiday.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
}
