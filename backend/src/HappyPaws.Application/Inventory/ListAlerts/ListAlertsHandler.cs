using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.ListAlerts;

/// <summary><c>GET /staff/inventory/alerts?branch=&amp;within_days=</c>.</summary>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
/// <param name="WithinDays">How far ahead "ใกล้หมดอายุ" looks, 1 to 365. 90 when absent.</param>
public sealed record ListAlertsQuery(string? BranchId = null, int? WithinDays = null);

/// <summary>
/// "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน" — what needs someone's attention, worst first.
/// <list type="bullet">
/// <item><c>expired</c>: a lot past its date that still has units on the books. This is how "ล็อตที่
/// ต้องเรียกคืน" is read: the contract has no way to mark a lot as recalled by its maker, so the lots
/// that must come off the shelf are the expired ones (<c>docs/assumptions.md § 42</c>).</item>
/// <item><c>out_of_stock</c>: an item the branch has stocked before and now has no usable units of.</item>
/// <item><c>near_expiry</c>: a lot that expires within the window, soonest first.</item>
/// <item><c>low_stock</c>: usable units at or below the item's reorder level.</item>
/// </list>
/// <para>
/// An item a branch has never held is not "out" there; it simply is not stocked there. Units already
/// on order are reported beside each row, so a low-stock row with enough on the way can be told from
/// one that needs an order.
/// </para>
/// </summary>
public sealed class ListAlertsHandler(
    IInventoryItemRepository items,
    IStockLedger ledger,
    IPurchaseOrderRepository orders,
    InventoryBranches branches,
    ClinicDays clinicDays)
{
    public const int DefaultWithinDays = 90;
    public const int MaxWithinDays = 365;

    public async Task<IReadOnlyList<InventoryAlertDto>> HandleAsync(ListAlertsQuery query, CancellationToken cancellationToken = default)
    {
        var window = query.WithinDays ?? DefaultWithinDays;

        if (window is < 1 or > MaxWithinDays)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["within_days"] = [$"within_days ต้องอยู่ระหว่าง 1 ถึง {MaxWithinDays}"],
            });
        }

        var scope = await branches.ForReadAsync(query.BranchId, cancellationToken);
        var lots = await ledger.LotsAsync(itemIds: null, scope, cancellationToken);

        if (lots.Count == 0)
        {
            return [];
        }

        var catalogue = (await items.FindManyAsync([.. lots.Select(lot => lot.ItemId).Distinct()], cancellationToken))
            .ToDictionary(item => item.Id);
        var onOrder = (await orders.OpenLinesAsync([.. catalogue.Keys], scope, cancellationToken))
            .GroupBy(line => (line.ItemId, line.BranchId))
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Outstanding));

        return Evaluate(lots, catalogue, onOrder, clinicDays.Today, window);
    }

    /// <summary>Pure: the alert list for these lots on this day.</summary>
    public static IReadOnlyList<InventoryAlertDto> Evaluate(
        IReadOnlyList<StockLot> lots,
        IReadOnlyDictionary<string, InventoryItem> catalogue,
        IReadOnlyDictionary<(string ItemId, string BranchId), int> onOrder,
        DateOnly today,
        int withinDays)
    {
        var alerts = new List<(InventoryAlertKind Kind, int Order, InventoryAlertDto Alert)>();
        var horizon = today.AddDays(withinDays);

        foreach (var group in lots.GroupBy(lot => (lot.ItemId, lot.BranchId)))
        {
            if (!catalogue.TryGetValue(group.Key.ItemId, out var item))
            {
                continue;
            }

            var available = StockAllocation.Available(group, today);
            var ordered = onOrder.GetValueOrDefault(group.Key);

            InventoryAlertDto Row(InventoryAlertKind kind, StockLot? lot = null) => new(
                WireEnum.Of(kind),
                item.Id,
                item.Name,
                item.Unit,
                group.Key.BranchId,
                available,
                item.ReorderLevel,
                ordered,
                lot?.Id,
                lot?.LotNumber,
                lot?.ExpiresOn,
                lot?.QuantityOnHand,
                lot?.ExpiresOn is { } expires ? expires.DayNumber - today.DayNumber : null);

            foreach (var lot in group.Where(lot => lot.QuantityOnHand > 0 && lot.ExpiresOn is not null))
            {
                var days = lot.ExpiresOn!.Value.DayNumber - today.DayNumber;

                if (lot.IsExpiredOn(today))
                {
                    alerts.Add((InventoryAlertKind.Expired, days, Row(InventoryAlertKind.Expired, lot)));
                }
                else if (lot.ExpiresOn <= horizon)
                {
                    alerts.Add((InventoryAlertKind.NearExpiry, days, Row(InventoryAlertKind.NearExpiry, lot)));
                }
            }

            switch (StockLevels.StatusOf(available, item.ReorderLevel))
            {
                case StockStatus.Out:
                    alerts.Add((InventoryAlertKind.OutOfStock, 0, Row(InventoryAlertKind.OutOfStock)));
                    break;
                case StockStatus.Low:
                    alerts.Add((InventoryAlertKind.LowStock, available, Row(InventoryAlertKind.LowStock)));
                    break;
            }
        }

        return
        [
            .. alerts
                .OrderBy(entry => entry.Kind)
                .ThenBy(entry => entry.Order)
                .ThenBy(entry => entry.Alert.ItemName, StringComparer.Ordinal)
                .ThenBy(entry => entry.Alert.BranchId, StringComparer.Ordinal)
                .Select(entry => entry.Alert),
        ];
    }
}
