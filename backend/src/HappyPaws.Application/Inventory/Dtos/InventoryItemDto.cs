namespace HappyPaws.Application.Inventory.Dtos;

/// <summary>
/// One row of <c>GET /staff/inventory/items</c> — "ยาและเวชภัณฑ์ · คงเหลือรายสาขา": the item, and what
/// each branch the caller may see holds of it.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>kind</c>: <c>medicine</c> or <c>supply</c>.</item>
/// <item><c>unit_price_satang</c>: What one unit is charged at when dispensed.</item>
/// <item><c>reorder_level</c>: At or below this many usable units a branch is "ใกล้หมด".</item>
/// <item><c>on_hand</c>: Every unit on the books across the listed branches, expired lots included.</item>
/// <item><c>available</c>: The usable part of <paramref name="OnHand"/>: lots not yet expired.</item>
/// <item><c>branches</c>: One entry per branch, in the order the branches are listed.</item>
/// </list>
/// </remarks>
public sealed record InventoryItemDto(
    string Id,
    uint Version,
    string Name,
    string Kind,
    string Unit,
    long UnitPriceSatang,
    int ReorderLevel,
    DateTimeOffset CreatedAt,
    int OnHand,
    int Available,
    IReadOnlyList<BranchStockDto> Branches);

/// <summary>One branch's stock of one item.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>status</c>: <c>ok</c>, <c>low</c> or <c>out</c>, judged on usable units.</item>
/// <item><c>on_order</c>: Units on purchase orders for this branch that have not arrived yet.</item>
/// <item><c>next_expiry</c>: The soonest expiry among lots that still hold usable units.</item>
/// <item><c>lots</c>: Lots that still hold units, soonest expiry first.</item>
/// <item><c>open_orders</c>: The purchase orders behind <paramref name="OnOrder"/>, for receiving against.</item>
/// </list>
/// </remarks>
public sealed record BranchStockDto(
    string BranchId,
    int OnHand,
    int Available,
    string Status,
    int OnOrder,
    DateOnly? NextExpiry,
    IReadOnlyList<StockLotDto> Lots,
    IReadOnlyList<OpenOrderDto> OpenOrders);

/// <remarks>
/// <list type="bullet">
/// <item><c>expired</c>: Past its expiry date: counted in <c>on_hand</c>, never issued on its own.</item>
/// </list>
/// </remarks>
public sealed record StockLotDto(string Id, string? LotNumber, DateOnly? ExpiresOn, int QuantityOnHand, bool Expired);

/// <remarks>
/// <list type="bullet">
/// <item><c>outstanding</c>: Ordered and not yet received.</item>
/// </list>
/// </remarks>
public sealed record OpenOrderDto(
    string PurchaseOrderId,
    string Number,
    string SupplierName,
    int Outstanding,
    DateOnly? ExpectedOn);
