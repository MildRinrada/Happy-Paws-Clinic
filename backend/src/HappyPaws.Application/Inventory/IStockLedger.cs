using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory;

/// <summary>One item's stock at one branch — the unit stock is locked by.</summary>
public sealed record StockKey(string ItemId, string BranchId);

/// <summary>
/// Persistence port for lots and the movement ledger.
/// <para>
/// <b>Every change to stock takes <see cref="LockAsync"/> first</b>, inside the caller's
/// transaction, and only then reads the lots it will change. The lock is per item per branch, so two
/// dispenses of the last box of amoxicillin queue behind each other and the second one sees what the
/// first one left, while a receipt of gauze at another branch does not wait at all. It is released
/// when the transaction ends. Reading the lots before taking it would decide on numbers another
/// transaction is about to change.
/// </para>
/// </summary>
public interface IStockLedger
{
    /// <summary>
    /// Serialises stock changes for these keys until the transaction ends. Several keys are locked in
    /// one fixed order, so two transfers in opposite directions cannot deadlock.
    /// </summary>
    Task LockAsync(IReadOnlyCollection<StockKey> keys, CancellationToken cancellationToken = default);

    /// <summary>Every lot of the item at the branch, tracked for change. Call after <see cref="LockAsync"/>.</summary>
    Task<IReadOnlyList<StockLot>> LotsForUpdateAsync(StockKey key, CancellationToken cancellationToken = default);

    void AddLot(StockLot lot);

    /// <summary>Appends a ledger row. There is no update and no delete.</summary>
    void Append(StockMovement movement);

    /// <summary>Lots for reading, narrowed by item and branch when given.</summary>
    Task<IReadOnlyList<StockLot>> LotsAsync(
        IReadOnlyCollection<string>? itemIds,
        IReadOnlyCollection<string>? branchIds,
        CancellationToken cancellationToken = default);
}
