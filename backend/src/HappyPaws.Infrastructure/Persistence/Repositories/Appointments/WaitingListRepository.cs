using HappyPaws.Application.Appointments;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Appointments;

internal sealed class WaitingListRepository(HappyPawsDbContext db) : IWaitingListRepository
{
    public void Add(WaitingListEntry entry) => db.Set<WaitingListEntry>().Add(entry);

    public Task<bool> IsWaitingAsync(
        string petId,
        string serviceId,
        DateOnly today,
        CancellationToken cancellationToken = default) =>
        db.Set<WaitingListEntry>().AnyAsync(
            entry => entry.PetId == petId && entry.ServiceId == serviceId && entry.LatestOn >= today,
            cancellationToken);

    public async Task<IReadOnlyList<WaitingListEntry>> WaitingForAsync(
        string serviceId,
        DateOnly on,
        CancellationToken cancellationToken = default) =>
        await db.Set<WaitingListEntry>()
            .Where(entry => entry.ServiceId == serviceId && entry.EarliestOn <= on && entry.LatestOn >= on)
            .OrderBy(entry => entry.CreatedAt).ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

    public async Task<CursorPage<WaitingListRow>> ListAsync(
        WaitingListFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var entries = db.Set<WaitingListEntry>().AsNoTracking();

        // An entry with no branch is reached by an opening at any branch, so it is everyone's.
        if (filter.VisibleBranchIds is { Count: > 0 } visible)
        {
            entries = entries.Where(entry => entry.BranchId == null || visible.Contains(entry.BranchId));
        }

        if (filter.BranchId is { Length: > 0 } branchId)
        {
            entries = entries.Where(entry => entry.BranchId == null || entry.BranchId == branchId);
        }

        if (filter.ServiceId is { Length: > 0 } serviceId)
        {
            entries = entries.Where(entry => entry.ServiceId == serviceId);
        }

        var today = filter.Today;
        entries = filter.Lapsed switch
        {
            true => entries.Where(entry => entry.LatestOn < today),
            false => entries.Where(entry => entry.LatestOn >= today),
            null => entries,
        };

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            entries = entries.Where(entry =>
                entry.CreatedAt > sortKey || (entry.CreatedAt == sortKey && string.Compare(entry.Id, id) > 0));
        }

        var rows = await (
                from entry in entries
                join pet in db.Set<Pet>() on entry.PetId equals pet.Id
                join service in db.Set<ClinicService>() on entry.ServiceId equals service.Id
                orderby entry.CreatedAt, entry.Id
                select new WaitingListRow(entry, pet.Name, service.Name))
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        if (rows.Count <= page.SafeLimit)
        {
            return new CursorPage<WaitingListRow>(rows, null);
        }

        var data = rows.Take(page.SafeLimit).ToList();
        var last = data[^1].Entry;

        return new CursorPage<WaitingListRow>(data, Cursor.Encode(last.CreatedAt, last.Id));
    }

    public Task<WaitingListEntry?> FindByIdAsync(string id, CancellationToken cancellationToken = default) =>
        db.Set<WaitingListEntry>().FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    public void Remove(WaitingListEntry entry) => db.Set<WaitingListEntry>().Remove(entry);
}
