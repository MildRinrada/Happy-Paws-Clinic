using System.Security.Cryptography;
using System.Text;
using HappyPaws.Application.Inventory;
using HappyPaws.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Inventory;

/// <summary>
/// Lots and the movement ledger, and the lock every stock change runs under.
/// <para>
/// <b>Why an advisory lock and not <c>SELECT … FOR UPDATE</c> on the lots.</b> A row lock can only
/// lock rows that exist. The first receipt of an item at a branch, or two receipts of a new lot
/// number arriving together, have no row to lock, and both would insert. A transaction-scoped
/// advisory lock keyed on the item and branch covers the rows that do not exist yet as well as the
/// ones that do, and is released when the transaction ends, commit or rollback.
/// </para>
/// <para>
/// The balance on each lot also carries the usual <c>xmin</c> version, and the table refuses a
/// negative balance, so even a writer that forgot the lock could not take stock below zero.
/// </para>
/// </summary>
internal sealed class StockLedger(HappyPawsDbContext db) : IStockLedger
{
    public async Task LockAsync(IReadOnlyCollection<StockKey> keys, CancellationToken cancellationToken = default)
    {
        // A fixed order across every caller: two transfers A→B and B→A take the same lock first, so
        // one waits for the other instead of each holding half.
        foreach (var key in keys.Select(LockKey).Distinct().Order())
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", [key], cancellationToken);
        }
    }

    public async Task<IReadOnlyList<StockLot>> LotsForUpdateAsync(StockKey key, CancellationToken cancellationToken = default) =>
        await db.Set<StockLot>()
            .Where(lot => lot.ItemId == key.ItemId && lot.BranchId == key.BranchId)
            .OrderBy(lot => lot.ReceivedAt).ThenBy(lot => lot.Id)
            .ToListAsync(cancellationToken);

    public void AddLot(StockLot lot) => db.Set<StockLot>().Add(lot);

    public void Append(StockMovement movement) => db.Set<StockMovement>().Add(movement);

    public async Task<IReadOnlyList<StockLot>> LotsAsync(
        IReadOnlyCollection<string>? itemIds,
        IReadOnlyCollection<string>? branchIds,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<StockLot>().AsNoTracking();

        if (itemIds is not null)
        {
            var items = itemIds.ToArray();
            query = query.Where(lot => items.Contains(lot.ItemId));
        }

        if (branchIds is not null)
        {
            var branches = branchIds.ToArray();
            query = query.Where(lot => branches.Contains(lot.BranchId));
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A stable 64-bit key per item and branch. Advisory locks share one namespace of integers with
    /// the document-number locks, so the hash is salted with what it is for.
    /// </summary>
    private static long LockKey(StockKey key) =>
        BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes($"stock:{key.ItemId}:{key.BranchId}")), 0);
}
