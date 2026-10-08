using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Inventory;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.Application.Inventory.Dispensing;

/// <summary>What a dispense did to stock.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>item</c>: The item the drug matched, or null.</item>
/// <item><c>available</c>: Usable units at the branch before the dispense; zero when nothing matched.</item>
/// <item><c>taken</c>: The ledger rows written, one per lot, when stock was deducted.</item>
/// </list>
/// </remarks>
public sealed record DispenseStockResult(
    DispenseStockStatus Status,
    InventoryItem? Item,
    int Available,
    IReadOnlyList<(StockMovement Movement, StockLot Lot)> Taken)
{
    public bool Deducted => Status == DispenseStockStatus.Deducted;

    public static DispenseStockResult NotMatched { get; } = new(DispenseStockStatus.NotMatched, null, 0, []);
}

/// <summary>
/// "ตัดสต๊อก" — takes a dispensed prescription off the dispensing branch's shelf.
/// <para>
/// <b>Matching.</b> The prescription's drug name is matched to the catalogue by
/// <see cref="InventoryItem.KeyOf"/>: spacing and case flattened, otherwise exact. The prescription
/// endpoint in the contract carries a drug name and no item reference, so the name is all there is,
/// and an exact match is the only one that cannot take the wrong drug off the shelf.
/// </para>
/// <para>
/// <b>Never blocks the dispense.</b> The drug has physically been handed over by the time this runs;
/// the contract says nothing about refusing a dispense for stock. So a drug that matches no item, or
/// a branch whose books show fewer usable units than were handed over, leaves stock untouched and
/// says so, rather than refusing or writing a ledger that goes below zero. Partial deduction is not
/// attempted: the books are already wrong, and taking "what there is" would hide by how much.
/// </para>
/// <para>
/// <b>Concurrency.</b> Under the item's stock lock at the branch, like every other stock change, so
/// two counters dispensing the last box at once are served one after the other: the first takes it,
/// the second finds nothing left and reports <see cref="DispenseStockStatus.Insufficient"/>.
/// </para>
/// </summary>
public sealed class StockDispenser(IInventoryItemRepository items, IStockLedger ledger, ClinicDays clinicDays)
{
    /// <summary>Call inside the dispense's transaction, after the prescription is marked dispensed.</summary>
    public async Task<DispenseStockResult> TakeAsync(
        Prescription prescription,
        string branchId,
        string staffId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var item = await items.FindByNameKeyAsync(InventoryItem.KeyOf(prescription.DrugName), cancellationToken);

        if (item is null)
        {
            return DispenseStockResult.NotMatched;
        }

        var key = new StockKey(item.Id, branchId);
        await ledger.LockAsync([key], cancellationToken);

        var lots = await ledger.LotsForUpdateAsync(key, cancellationToken);
        var today = clinicDays.Today;
        var available = StockAllocation.Available(lots, today);
        var plan = StockAllocation.Plan(lots, prescription.Quantity, today);

        if (plan is null)
        {
            return new DispenseStockResult(DispenseStockStatus.Insufficient, item, available, []);
        }

        var taken = new List<(StockMovement, StockLot)>();

        foreach (var take in plan)
        {
            var movement = StockMovement.Dispense(take.Lot, take.Quantity, prescription.Id, staffId, now);
            ledger.Append(movement);
            taken.Add((movement, take.Lot));
        }

        return new DispenseStockResult(DispenseStockStatus.Deducted, item, available, taken);
    }
}
