using HappyPaws.Application.Appointments;
using HappyPaws.Domain.Appointments;
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
}
