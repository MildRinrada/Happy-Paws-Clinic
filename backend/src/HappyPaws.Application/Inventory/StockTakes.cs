using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory;

/// <summary>
/// Chooses the lots a hand-made issue or transfer takes from, or refuses.
/// <para>
/// Unlike a dispense, a manual movement that asks for more than is there is refused with
/// <c>409 insufficient_stock</c>: nothing physical has happened yet, so the right answer is "count
/// again", not a ledger that goes below zero.
/// </para>
/// </summary>
public static class StockTakes
{
    public static IReadOnlyList<LotTake> Choose(
        IReadOnlyList<StockLot> lots,
        string? lotId,
        int quantity,
        DateOnly today)
    {
        if (lotId is { Length: > 0 })
        {
            // A lot named on purpose may be expired: that is how an expired lot leaves the shelf.
            var lot = lots.FirstOrDefault(candidate => candidate.Id == lotId.Trim())
                ?? throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["lot_id"] = ["ไม่พบล็อตนี้ของรายการนี้ในสาขา"],
                });

            return quantity <= lot.QuantityOnHand
                ? [new LotTake(lot, quantity)]
                : throw Insufficient(lot.QuantityOnHand);
        }

        return StockAllocation.Plan(lots, quantity, today) ?? throw Insufficient(StockAllocation.Available(lots, today));
    }

    public static ConflictException Insufficient(int available) =>
        new(ErrorCodes.InsufficientStock, $"สต๊อกไม่พอ ใช้ได้ {available}")
        {
            MessageEn = "Not enough usable stock.",
            Details = new Dictionary<string, object> { ["available"] = available },
        };
}
