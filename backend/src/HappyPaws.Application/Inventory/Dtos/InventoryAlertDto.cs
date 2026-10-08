namespace HappyPaws.Application.Inventory.Dtos;

/// <summary>
/// One row of <c>GET /staff/inventory/alerts</c> — "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน".
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>kind</c>: <c>expired</c>, <c>out_of_stock</c>, <c>near_expiry</c> or <c>low_stock</c>.</item>
/// <item><c>available</c>: Usable units of the item at the branch.</item>
/// <item><c>on_order</c>: Units already ordered for the branch and not yet received.</item>
/// <item><c>lot_id</c>: The lot, on the two expiry kinds.</item>
/// <item><c>lot_quantity</c>: Units left in that lot.</item>
/// <item><c>days_to_expiry</c>: Clinic days until the lot expires; negative once it has.</item>
/// </list>
/// </remarks>
public sealed record InventoryAlertDto(
    string Kind,
    string ItemId,
    string ItemName,
    string Unit,
    string BranchId,
    int Available,
    int ReorderLevel,
    int OnOrder,
    string? LotId = null,
    string? LotNumber = null,
    DateOnly? ExpiresOn = null,
    int? LotQuantity = null,
    int? DaysToExpiry = null);
