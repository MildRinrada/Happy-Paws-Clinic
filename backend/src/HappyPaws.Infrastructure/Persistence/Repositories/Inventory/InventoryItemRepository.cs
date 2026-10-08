using HappyPaws.Application.Inventory;
using HappyPaws.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Inventory;

internal sealed class InventoryItemRepository(HappyPawsDbContext db) : IInventoryItemRepository
{
    public void Add(InventoryItem item) => db.Set<InventoryItem>().Add(item);

    public Task<InventoryItem?> FindAsync(string itemId, CancellationToken cancellationToken = default) =>
        db.Set<InventoryItem>().FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

    public Task<InventoryItem?> FindByNameKeyAsync(string nameKey, CancellationToken cancellationToken = default) =>
        nameKey.Length == 0
            ? Task.FromResult<InventoryItem?>(null)
            : db.Set<InventoryItem>().FirstOrDefaultAsync(item => item.NameKey == nameKey, cancellationToken);

    public async Task<IReadOnlyList<InventoryItem>> FindManyAsync(
        IReadOnlyCollection<string> itemIds,
        CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0)
        {
            return [];
        }

        var ids = itemIds.ToArray();

        return await db.Set<InventoryItem>().AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = db.Set<InventoryItem>().AsNoTracking();

        if (search is { Length: > 0 })
        {
            // LIKE's own wildcards in the search are taken literally.
            var pattern = $"%{search.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
            query = query.Where(item => EF.Functions.ILike(item.Name, pattern));
        }

        return await query.OrderBy(item => item.NameKey).ThenBy(item => item.Id).ToListAsync(cancellationToken);
    }
}
