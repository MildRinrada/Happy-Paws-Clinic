using HappyPaws.Domain.Common;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.UnitTests.Domain.Inventory;

/// <summary>
/// <c>docs/contract-extensions.md § 25</c>: editing and retiring a catalogue item, and cancelling
/// what is left of a purchase order.
/// </summary>
public class ItemAndOrderManagementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 10, 0, 0, TimeSpan.FromHours(7));

    private static InventoryItem Item() => InventoryItem.Create("Amoxicillin 250mg", InventoryItemKind.Medicine, "เม็ด", 500, 50, "stf_1");

    [Fact]
    public void A_rename_moves_the_match_key_with_it()
    {
        var item = Item();

        item.Rename("  Amoxicillin   250 mg ");

        Assert.Equal("Amoxicillin 250 mg", item.Name);
        Assert.True(item.IsNamedBy("amoxicillin 250 mg"));
        Assert.False(item.IsNamedBy("Amoxicillin 250mg"));
    }

    [Fact]
    public void Edits_keep_the_creation_rules()
    {
        var item = Item();

        Assert.Throws<DomainException>(() => item.Rename(" "));
        Assert.Throws<DomainException>(() => item.ChangeUnit(""));
        Assert.Throws<DomainException>(() => item.Reprice(-1));
        Assert.Throws<DomainException>(() => item.ChangeReorderLevel(-1));

        item.Reprice(650);
        item.ChangeUnit("แคปซูล");
        item.ChangeReorderLevel(0);

        Assert.Equal(650, item.UnitPriceSatang);
        Assert.Equal("แคปซูล", item.Unit);
        Assert.Equal(0, item.ReorderLevel);
    }

    [Fact]
    public void A_retired_item_refuses_new_use_until_it_is_brought_back()
    {
        var item = Item();

        item.Retire(Now);
        item.Retire(Now.AddDays(1));

        Assert.Equal(Now, item.RetiredAt);
        Assert.Equal("inventory_item_retired", Assert.Throws<DomainException>(item.EnsureInUse).Code);

        item.Reactivate();

        Assert.False(item.IsRetired);
        item.EnsureInUse();
    }

    [Fact]
    public void Cancelling_calls_off_the_remainder_and_keeps_what_arrived()
    {
        var order = PurchaseOrder.Raise("PO-202610-0009", "brn_1", "ซัพพลายเออร์", "stf_1", Now);
        order.AddLine("itm_a", 10, 250);
        order.AddLine("itm_b", 5, 900);
        order.Receive("itm_a", 4);

        order.Cancel("stf_2", " ของหมด ", Now.AddDays(1));

        Assert.Equal(PurchaseOrderStatus.Cancelled, order.Status);
        Assert.Equal("ของหมด", order.CancelReason);
        Assert.Equal(4, order.LineFor("itm_a")!.ReceivedQuantity);
        Assert.Equal("state_conflict", Assert.Throws<StateConflictException>(() => order.Receive("itm_a", 1)).Code);
        Assert.Throws<StateConflictException>(() => order.Cancel("stf_2", null, Now));
    }

    [Fact]
    public void An_order_that_fully_arrived_has_nothing_to_cancel()
    {
        var order = PurchaseOrder.Raise("PO-202610-0010", "brn_1", "ซัพพลายเออร์", "stf_1", Now);
        order.AddLine("itm_a", 2, 250);
        order.Receive("itm_a", 2);

        Assert.Throws<StateConflictException>(() => order.Cancel("stf_1", null, Now));
    }
}
