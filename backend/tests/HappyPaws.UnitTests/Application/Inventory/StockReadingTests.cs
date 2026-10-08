using HappyPaws.Application.Inventory;
using HappyPaws.Application.Inventory.ListAlerts;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.UnitTests.Application.Inventory;

/// <summary>What the stock list and the alert list make of the same lots.</summary>
public class StockReadingTests
{
    private static readonly DateOnly Today = new(2026, 10, 7);
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 10, 0, 0, TimeSpan.FromHours(7));

    private static InventoryItem Item(string name, int reorder) =>
        InventoryItem.Create(name, InventoryItemKind.Medicine, "เม็ด", 500, reorder, "stf_1");

    private static StockLot Lot(InventoryItem item, string branch, int quantity, DateOnly? expires, string? number = null)
    {
        var lot = StockLot.Open(item.Id, branch, number, expires, Now.AddDays(-30));

        if (quantity > 0)
        {
            StockMovement.Receive(lot, quantity, "stf_1", Now.AddDays(-30));
        }

        return lot;
    }

    [Fact]
    public void The_list_shows_every_branch_in_scope_with_usable_units_and_what_is_on_order()
    {
        var item = Item("Amoxicillin", reorder: 5);
        var lots = new[]
        {
            Lot(item, "brn_a", 4, Today.AddDays(40), "A"),
            Lot(item, "brn_a", 10, Today.AddDays(-2), "OLD"),
            Lot(item, "brn_b", 0, null),
        };
        var open = new[] { new OpenOrderLine("po_1", "PO-202610-0001", "brn_a", "ซัพพลายเออร์", item.Id, 20, Today.AddDays(3)) };

        var dto = Assert.Single(StockView.Compose([item], ["brn_a", "brn_b", "brn_c"], lots, open, Today));

        Assert.Equal(["brn_a", "brn_b", "brn_c"], dto.Branches.Select(branch => branch.BranchId));

        var a = dto.Branches[0];
        Assert.Equal(14, a.OnHand);
        Assert.Equal(4, a.Available);
        Assert.Equal("low", a.Status);
        Assert.Equal(20, a.OnOrder);
        Assert.Equal(Today.AddDays(40), a.NextExpiry);
        Assert.Equal(["A", "OLD"], a.Lots.OrderBy(lot => lot.LotNumber).Select(lot => lot.LotNumber));
        Assert.True(a.Lots.Single(lot => lot.LotNumber == "OLD").Expired);

        Assert.Equal("out", dto.Branches[1].Status);
        Assert.Empty(dto.Branches[1].Lots);
        Assert.Equal(0, dto.Branches[2].OnHand);
        Assert.Equal(14, dto.OnHand);
        Assert.Equal(4, dto.Available);
    }

    [Fact]
    public void Alerts_come_worst_first_and_ignore_items_a_branch_never_stocked()
    {
        var low = Item("Low", reorder: 10);
        var near = Item("Near", reorder: 0);
        var stale = Item("Stale", reorder: 0);
        var gone = Item("Gone", reorder: 0);

        var lots = new[]
        {
            Lot(low, "brn_a", 4, null),
            Lot(near, "brn_a", 20, Today.AddDays(12), "N"),
            Lot(near, "brn_a", 20, Today.AddDays(200), "FAR"),
            Lot(stale, "brn_a", 3, Today.AddDays(-5), "S"),
            Lot(stale, "brn_a", 8, null),
            Lot(gone, "brn_a", 0, null),
        };
        var catalogue = new[] { low, near, stale, gone }.ToDictionary(item => item.Id);
        var onOrder = new Dictionary<(string, string), int> { [(low.Id, "brn_a")] = 30 };

        var alerts = ListAlertsHandler.Evaluate(lots, catalogue, onOrder, Today, withinDays: 30);

        Assert.Equal(
            ["expired:Stale", "out_of_stock:Gone", "near_expiry:Near", "low_stock:Low"],
            alerts.Select(alert => $"{alert.Kind}:{alert.ItemName}"));

        Assert.Equal(-5, alerts[0].DaysToExpiry);
        Assert.Equal(12, alerts[2].DaysToExpiry);
        Assert.Equal("N", alerts[2].LotNumber);
        Assert.Equal(30, alerts[3].OnOrder);
        Assert.Equal(4, alerts[3].Available);
    }
}
