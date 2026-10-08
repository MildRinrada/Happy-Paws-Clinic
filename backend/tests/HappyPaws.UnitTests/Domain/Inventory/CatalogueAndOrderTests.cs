using HappyPaws.Domain.Common;
using HappyPaws.Domain.Inventory;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.UnitTests.Domain.Inventory;

/// <summary>
/// How a prescription finds its item, how an order is received in parts, and what a dispense
/// records on the prescription.
/// </summary>
public class CatalogueAndOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 10, 0, 0, TimeSpan.FromHours(7));

    [Theory]
    [InlineData("Amoxicillin 250mg", "amoxicillin 250mg", true)]
    [InlineData("Amoxicillin 250mg", "  AMOXICILLIN   250mg ", true)]
    [InlineData("Amoxicillin 250mg", "Amoxicillin 250 mg", false)]
    [InlineData("Amoxicillin 250mg", "Amoxicillin", false)]
    [InlineData("ยาแก้อักเสบ 500", "ยาแก้อักเสบ  500", true)]
    public void A_prescription_names_an_item_only_by_its_whole_name(string itemName, string drugName, bool matches)
    {
        var item = InventoryItem.Create(itemName, InventoryItemKind.Medicine, "เม็ด", 500, 0, "stf_1");

        Assert.Equal(matches, item.IsNamedBy(drugName));
    }

    [Theory]
    [InlineData("", "เม็ด", 0, 0)]
    [InlineData("ยา", "", 0, 0)]
    [InlineData("ยา", "เม็ด", -1, 0)]
    [InlineData("ยา", "เม็ด", 0, -1)]
    public void An_item_needs_a_name_a_unit_and_no_negative_numbers(string name, string unit, long price, int reorder)
    {
        Assert.Throws<DomainException>(() => InventoryItem.Create(name, InventoryItemKind.Medicine, unit, price, reorder, "stf_1"));
    }

    [Fact]
    public void An_order_is_received_in_parts_and_never_past_what_was_ordered()
    {
        var order = PurchaseOrder.Raise("PO-202610-0001", "brn_1", "ซัพพลายเออร์", "stf_1", Now);
        order.AddLine("itm_a", 10, 250);
        order.AddLine("itm_b", 5, 900);

        Assert.Equal((10 * 250) + (5 * 900), order.TotalCost.Satang);
        Assert.Equal(PurchaseOrderStatus.Ordered, order.Status);

        order.Receive("itm_a", 10);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, order.Status);

        var over = Assert.Throws<DomainException>(() => order.Receive("itm_b", 6));
        Assert.Equal("over_receipt", over.Code);

        order.Receive("itm_b", 5);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);

        Assert.Throws<DomainException>(() => order.Receive("itm_c", 1));
        Assert.Throws<DomainException>(() => order.AddLine("itm_a", 1, 1));
    }

    [Fact]
    public void A_dispense_records_the_item_whether_stock_was_taken_and_the_price_charged()
    {
        var prescription = Prescription.Write("con_1", "pet_1", "stf_1", Now, "Meloxicam 1mg", "1 เม็ด", "วันละครั้ง", 5, 5);

        Assert.Throws<StateConflictException>(() => prescription.RecordDispenseOutcome("itm_1", true, 800));

        prescription.Dispense("stf_2", Now);
        prescription.RecordDispenseOutcome("itm_1", stockDeducted: false, chargedUnitPriceSatang: 800);

        Assert.Equal("itm_1", prescription.InventoryItemId);
        Assert.False(prescription.StockDeducted);
        Assert.Equal(4_000, prescription.ChargedSatang);
    }

    [Fact]
    public void A_free_item_or_no_item_charges_nothing_and_stock_cannot_be_taken_without_an_item()
    {
        var free = Prescription.Write("con_1", "pet_1", "stf_1", Now, "Vitamin", "1 เม็ด", "วันละครั้ง", 5, 5);
        free.Dispense("stf_2", Now);
        free.RecordDispenseOutcome("itm_1", stockDeducted: true, chargedUnitPriceSatang: 0);

        Assert.Null(free.ChargedUnitPriceSatang);
        Assert.Equal(0, free.ChargedSatang);

        var unknown = Prescription.Write("con_1", "pet_1", "stf_1", Now, "Unknown", "1 เม็ด", "วันละครั้ง", 5, 5);
        unknown.Dispense("stf_2", Now);

        Assert.Throws<DomainException>(() => unknown.RecordDispenseOutcome(null, stockDeducted: true, chargedUnitPriceSatang: null));
    }
}
