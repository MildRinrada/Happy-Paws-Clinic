namespace HappyPaws.Domain.Inventory;

/// <summary>How many units of one lot an issue takes.</summary>
public sealed record LotTake(StockLot Lot, int Quantity);

/// <summary>
/// Which lots an issue, a transfer or a dispense takes from: first expiry, first out.
/// <para>
/// The lot that expires soonest goes first, so stock is used before it spoils; lots without an expiry
/// date go last, then the oldest receipt first. Expired lots are never chosen here. They stay on the
/// books, and on the alert list, until someone issues them by lot on purpose, because handing an
/// owner an expired drug is not a thing the system should do on its own.
/// </para>
/// </summary>
public static class StockAllocation
{
    /// <summary>The usable units across these lots: everything not yet expired.</summary>
    public static int Available(IEnumerable<StockLot> lots, DateOnly today) =>
        lots.Where(lot => !lot.IsExpiredOn(today)).Sum(lot => lot.QuantityOnHand);

    /// <summary>
    /// The lots to take <paramref name="quantity"/> from, or null when the usable stock is short.
    /// All or nothing: a plan never covers part of the request.
    /// </summary>
    public static IReadOnlyList<LotTake>? Plan(IEnumerable<StockLot> lots, int quantity, DateOnly today)
    {
        if (quantity <= 0)
        {
            return [];
        }

        var plan = new List<LotTake>();
        var remaining = quantity;

        foreach (var lot in Order(lots, today))
        {
            var take = Math.Min(remaining, lot.QuantityOnHand);
            plan.Add(new LotTake(lot, take));
            remaining -= take;

            if (remaining == 0)
            {
                return plan;
            }
        }

        return null;
    }

    private static IEnumerable<StockLot> Order(IEnumerable<StockLot> lots, DateOnly today) =>
        lots
            .Where(lot => lot.QuantityOnHand > 0 && !lot.IsExpiredOn(today))
            .OrderBy(lot => lot.ExpiresOn is null)
            .ThenBy(lot => lot.ExpiresOn)
            .ThenBy(lot => lot.ReceivedAt)
            .ThenBy(lot => lot.Id, StringComparer.Ordinal);
}

/// <summary>A branch's position on one item, for the list and the alerts.</summary>
public enum StockStatus
{
    /// <summary>More usable units than the reorder level.</summary>
    Ok,

    /// <summary>"ใกล้หมด" — some left, at or below the reorder level.</summary>
    Low,

    /// <summary>No usable units at all.</summary>
    Out,
}

public static class StockLevels
{
    public static StockStatus StatusOf(int available, int reorderLevel) =>
        available <= 0 ? StockStatus.Out
        : available <= reorderLevel ? StockStatus.Low
        : StockStatus.Ok;
}
