using HappyPaws.Application.Inventory;
using HappyPaws.Application.Inventory.AdjustStock;
using HappyPaws.Application.Inventory.CreatePurchaseOrder;
using HappyPaws.Application.Inventory.RecordMovement;

namespace HappyPaws.Api.Contracts.Inventory;

/// <summary>
/// A new catalogue entry, on a receipt or a purchase-order line:
/// <code>{ "name": "Amoxicillin 250mg", "kind": "medicine", "unit": "เม็ด", "unit_price_satang": 500, "reorder_level": 50 }</code>
/// </summary>
public sealed record NewItemRequest(
    string? Name = null,
    string? Kind = null,
    string? Unit = null,
    long? UnitPriceSatang = null,
    int? ReorderLevel = null)
{
    public NewItemInput ToInput() => new(Name, Kind, Unit, UnitPriceSatang, ReorderLevel);
}

/// <summary>
/// <c>POST /staff/inventory/movements</c> — "รับเข้า จ่ายออก โอนสาขา · อ้างอิงเอกสารต้นทาง":
/// <code>
/// { "type": "receive", "branch_id": "brn_...", "item_id": "itm_...", "quantity": 100,
///   "lot_number": "A2310", "expires_on": "2027-06-30", "unit_cost_satang": 250,
///   "purchase_order_id": "po_...", "reference": "DN-55120" }
/// { "type": "issue", "item_id": "itm_...", "quantity": 2, "note": "ใช้ในห้องผ่าตัด" }
/// { "type": "transfer", "branch_id": "brn_a", "to_branch_id": "brn_b", "item_id": "itm_...", "quantity": 20 }
/// </code>
/// A receipt may describe a new item in <c>new_item</c> instead of <c>item_id</c>. Issues and
/// transfers take first expiry first unless <c>lot_id</c> names a lot.
/// </summary>
public sealed record RecordMovementRequest(
    string? Type = null,
    string? BranchId = null,
    string? ToBranchId = null,
    string? ItemId = null,
    NewItemRequest? NewItem = null,
    int? Quantity = null,
    string? LotId = null,
    string? LotNumber = null,
    DateOnly? ExpiresOn = null,
    long? UnitCostSatang = null,
    string? PurchaseOrderId = null,
    string? Reference = null,
    string? Note = null)
{
    public RecordMovementCommand ToCommand() => new(
        Type,
        BranchId,
        ToBranchId,
        ItemId,
        NewItem?.ToInput(),
        Quantity,
        LotId,
        LotNumber,
        ExpiresOn,
        UnitCostSatang,
        PurchaseOrderId,
        Reference,
        Note);
}

/// <summary>
/// <c>POST /staff/inventory/adjustments</c> — "ปรับยอดหลังนับสต๊อก · ต้องระบุเหตุผล":
/// <code>{ "branch_id": "brn_...", "item_id": "itm_...", "lot_id": "lot_...", "counted_quantity": 37, "reason": "นับสต๊อกสิ้นเดือน" }</code>
/// </summary>
public sealed record AdjustStockRequest(
    string? BranchId = null,
    string? ItemId = null,
    string? LotId = null,
    int? CountedQuantity = null,
    string? Reason = null)
{
    public AdjustStockCommand ToCommand() => new(BranchId, ItemId, LotId, CountedQuantity, Reason);
}

/// <summary>
/// <c>POST /staff/inventory/purchase-orders</c> — "สั่งซื้อจากซัพพลายเออร์ · รับของบางส่วนได้":
/// <code>
/// { "branch_id": "brn_...", "supplier_name": "บริษัท ยาสัตว์ จำกัด", "expected_on": "2026-10-20",
///   "lines": [ { "item_id": "itm_...", "quantity": 200, "unit_cost_satang": 250 },
///              { "new_item": { "name": "Meloxicam 1mg", "unit": "เม็ด", "unit_price_satang": 800 },
///                "quantity": 100, "unit_cost_satang": 300 } ] }
/// </code>
/// </summary>
public sealed record CreatePurchaseOrderRequest(
    string? BranchId = null,
    string? SupplierName = null,
    DateOnly? ExpectedOn = null,
    string? Note = null,
    IReadOnlyList<PurchaseOrderLineRequest>? Lines = null)
{
    public CreatePurchaseOrderCommand ToCommand() => new(
        BranchId,
        SupplierName,
        ExpectedOn,
        Note,
        Lines?.Select(line => new PurchaseOrderLineInput(line.ItemId, line.NewItem?.ToInput(), line.Quantity, line.UnitCostSatang)).ToList());
}

public sealed record PurchaseOrderLineRequest(
    string? ItemId = null,
    NewItemRequest? NewItem = null,
    int? Quantity = null,
    long? UnitCostSatang = null);
