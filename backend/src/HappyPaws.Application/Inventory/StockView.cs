using HappyPaws.Application.Common;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory;

/// <summary>
/// Turns lots and open order lines into the stock shown for an item, branch by branch.
/// <para>
/// One builder for the list and for the answer of every stock change, so a receipt answers with
/// exactly the numbers the list will show a second later. Usable units exclude expired lots, and the
/// status is judged on them: a shelf of expired tablets is not stock.
/// </para>
/// </summary>
public sealed class StockView(
    IInventoryItemRepository items,
    IStockLedger ledger,
    IPurchaseOrderRepository orders,
    ClinicDays clinicDays)
{
    /// <summary>The item's stock at these branches, read fresh.</summary>
    public async Task<InventoryItemDto> ForAsync(
        string itemId,
        IReadOnlyList<string> branchIds,
        CancellationToken cancellationToken = default)
    {
        var item = await items.FindAsync(itemId, cancellationToken)
            ?? throw new InvalidOperationException($"inventory item {itemId} vanished");

        return (await BuildAsync([item], branchIds, cancellationToken))[0];
    }

    public async Task<IReadOnlyList<InventoryItemDto>> BuildAsync(
        IReadOnlyList<InventoryItem> catalogue,
        IReadOnlyList<string> branchIds,
        CancellationToken cancellationToken = default)
    {
        if (catalogue.Count == 0)
        {
            return [];
        }

        var itemIds = catalogue.Select(item => item.Id).ToList();
        var lots = await ledger.LotsAsync(itemIds, branchIds, cancellationToken);
        var open = await orders.OpenLinesAsync(itemIds, branchIds, cancellationToken);

        return Compose(catalogue, branchIds, lots, open, clinicDays.Today);
    }

    /// <summary>Pure: the same inputs always draw the same stock.</summary>
    public static IReadOnlyList<InventoryItemDto> Compose(
        IReadOnlyList<InventoryItem> catalogue,
        IReadOnlyList<string> branchIds,
        IReadOnlyList<StockLot> lots,
        IReadOnlyList<OpenOrderLine> openLines,
        DateOnly today)
    {
        var lotsByKey = lots.ToLookup(lot => (lot.ItemId, lot.BranchId));
        var linesByKey = openLines.ToLookup(line => (line.ItemId, line.BranchId));

        return
        [
            .. catalogue.Select(item =>
            {
                var branches = branchIds
                    .Select(branchId => Branch(item, branchId, lotsByKey[(item.Id, branchId)], linesByKey[(item.Id, branchId)], today))
                    .ToList();

                return new InventoryItemDto(
                    item.Id,
                    item.Version,
                    item.Name,
                    WireEnum.Of(item.Kind),
                    item.Unit,
                    item.UnitPriceSatang,
                    item.ReorderLevel,
                    item.CreatedAt,
                    branches.Sum(branch => branch.OnHand),
                    branches.Sum(branch => branch.Available),
                    branches,
                    !item.IsRetired,
                    item.RetiredAt);
            }),
        ];
    }

    private static BranchStockDto Branch(
        InventoryItem item,
        string branchId,
        IEnumerable<StockLot> lots,
        IEnumerable<OpenOrderLine> openLines,
        DateOnly today)
    {
        var held = lots
            .Where(lot => lot.QuantityOnHand > 0)
            .OrderBy(lot => lot.ExpiresOn is null)
            .ThenBy(lot => lot.ExpiresOn)
            .ThenBy(lot => lot.ReceivedAt)
            .ToList();

        var available = StockAllocation.Available(held, today);
        var orders = openLines.OrderBy(line => line.ExpectedOn is null).ThenBy(line => line.ExpectedOn).ToList();

        return new BranchStockDto(
            branchId,
            held.Sum(lot => lot.QuantityOnHand),
            available,
            WireEnum.Of(StockLevels.StatusOf(available, item.ReorderLevel)),
            orders.Sum(line => line.Outstanding),
            held.Where(lot => !lot.IsExpiredOn(today)).Select(lot => lot.ExpiresOn).FirstOrDefault(expires => expires is not null),
            [.. held.Select(lot => new StockLotDto(lot.Id, lot.LotNumber, lot.ExpiresOn, lot.QuantityOnHand, lot.IsExpiredOn(today)))],
            [.. orders.Select(line => new OpenOrderDto(line.PurchaseOrderId, line.Number, line.SupplierName, line.Outstanding, line.ExpectedOn))]);
    }
}
