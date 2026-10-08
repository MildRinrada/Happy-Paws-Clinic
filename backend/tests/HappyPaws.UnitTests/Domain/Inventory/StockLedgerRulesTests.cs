using HappyPaws.Domain.Common;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.UnitTests.Domain.Inventory;

/// <summary>
/// The rules a lot and its ledger keep between them: first expiry first, never below zero, and every
/// change to a balance written as a row of the same size.
/// </summary>
public class StockLedgerRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 7);
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 10, 0, 0, TimeSpan.FromHours(7));

    private static StockLot Lot(int quantity, DateOnly? expires, string? number = null, int receivedDaysAgo = 10)
    {
        var lot = StockLot.Open("itm_1", "brn_1", number, expires, Now.AddDays(-receivedDaysAgo));

        if (quantity > 0)
        {
            StockMovement.Receive(lot, quantity, "stf_1", Now.AddDays(-receivedDaysAgo));
        }

        return lot;
    }

    [Fact]
    public void A_plan_takes_the_soonest_expiry_first_and_lots_without_one_last()
    {
        var never = Lot(5, null, "N");
        var late = Lot(5, Today.AddDays(100), "L");
        var soon = Lot(3, Today.AddDays(5), "S");

        var plan = StockAllocation.Plan([never, late, soon], 10, Today)!;

        Assert.Equal(["S", "L", "N"], plan.Select(take => take.Lot.LotNumber));
        Assert.Equal([3, 5, 2], plan.Select(take => take.Quantity));
    }

    [Fact]
    public void An_expired_lot_is_never_planned_and_does_not_count_as_available()
    {
        var expired = Lot(50, Today.AddDays(-1), "X");
        var good = Lot(4, Today, "G");

        Assert.Equal(4, StockAllocation.Available([expired, good], Today));
        Assert.Null(StockAllocation.Plan([expired, good], 5, Today));
        Assert.Equal("G", Assert.Single(StockAllocation.Plan([expired, good], 4, Today)!).Lot.LotNumber);
    }

    [Fact]
    public void A_plan_is_all_or_nothing()
    {
        Assert.Null(StockAllocation.Plan([Lot(2, null), Lot(2, null)], 5, Today));
    }

    [Fact]
    public void A_lot_refuses_to_go_below_zero_and_the_balance_is_the_sum_of_its_rows()
    {
        var lot = Lot(5, null);
        var rows = new List<StockMovement>
        {
            StockMovement.Issue(lot, 2, "stf_1", Now),
            StockMovement.Dispense(lot, 3, "rx_1", "stf_1", Now),
        };

        var refused = Assert.Throws<DomainException>(() => StockMovement.Issue(lot, 1, "stf_1", Now));

        Assert.Equal("insufficient_stock", refused.Code);
        Assert.Equal(0, lot.QuantityOnHand);
        Assert.Equal(5 - 2 - 3, rows.Sum(row => row.Quantity) + 5);
        Assert.Equal("rx_1", rows[1].PrescriptionId);
        Assert.Equal(StockMovementKind.Dispense, rows[1].Kind);
        Assert.Equal(0, rows[1].BalanceAfter);
    }

    [Fact]
    public void A_count_sets_the_balance_and_records_the_signed_difference_with_its_reason()
    {
        var lot = Lot(10, null);

        var down = StockMovement.Adjust(lot, 7, "เม็ดแตก", "stf_1", Now);
        var up = StockMovement.Adjust(lot, 9, "พบอีกกล่อง", "stf_1", Now);

        Assert.Equal(-3, down.Quantity);
        Assert.Equal(2, up.Quantity);
        Assert.Equal(9, lot.QuantityOnHand);
        Assert.Equal("เม็ดแตก", down.Note);
        Assert.Throws<DomainException>(() => StockMovement.Adjust(lot, 1, " ", "stf_1", Now));
        Assert.Throws<DomainException>(() => StockMovement.Adjust(lot, -1, "นับ", "stf_1", Now));
    }

    [Fact]
    public void A_transfer_writes_a_pair_of_rows_that_cancel_out()
    {
        var from = Lot(10, Today.AddDays(30), "T");
        var to = StockLot.Open("itm_1", "brn_2", "T", Today.AddDays(30), Now);

        var outgoing = StockMovement.Transfer(from, 4, outgoing: true, "trf_1", "stf_1", Now);
        var incoming = StockMovement.Transfer(to, 4, outgoing: false, "trf_1", "stf_1", Now);

        Assert.Equal(0, outgoing.Quantity + incoming.Quantity);
        Assert.Equal(StockMovementKind.TransferOut, outgoing.Kind);
        Assert.Equal(StockMovementKind.TransferIn, incoming.Kind);
        Assert.Equal(6, from.QuantityOnHand);
        Assert.Equal(4, to.QuantityOnHand);
    }

    [Fact]
    public void A_lot_matches_a_receipt_of_the_same_number_and_expiry_only()
    {
        var lot = Lot(1, Today.AddDays(30), "A1");

        Assert.True(lot.Matches(" A1 ", Today.AddDays(30)));
        Assert.False(lot.Matches("A1", Today.AddDays(31)));
        Assert.False(lot.Matches(null, Today.AddDays(30)));
        Assert.True(Lot(0, null).Matches("  ", null));
    }

    [Theory]
    [InlineData(0, 10, StockStatus.Out)]
    [InlineData(10, 10, StockStatus.Low)]
    [InlineData(11, 10, StockStatus.Ok)]
    [InlineData(1, 0, StockStatus.Ok)]
    public void Status_is_judged_on_usable_units_against_the_reorder_level(int available, int reorder, StockStatus expected)
    {
        Assert.Equal(expected, StockLevels.StatusOf(available, reorder));
    }
}
