using HappyPaws.Application.Common;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.Dtos;

/// <summary>One ledger row, as written.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>type</c>: <c>receive</c>, <c>issue</c>, <c>transfer_out</c>, <c>transfer_in</c>, <c>adjustment</c> or <c>dispense</c>.</item>
/// <item><c>quantity</c>: Signed: positive in, negative out.</item>
/// <item><c>balance_after</c>: The lot's balance once this row was written.</item>
/// </list>
/// </remarks>
public sealed record StockMovementDto(
    string Id,
    string Type,
    string ItemId,
    string BranchId,
    string LotId,
    string? LotNumber,
    DateOnly? ExpiresOn,
    int Quantity,
    int BalanceAfter,
    DateTimeOffset OccurredAt,
    string RecordedByStaffId,
    string? Reference,
    string? Note,
    long? UnitCostSatang,
    string? PurchaseOrderId,
    string? PrescriptionId,
    string? TransferId)
{
    public static StockMovementDto From(StockMovement movement, StockLot lot) =>
        new(
            movement.Id,
            WireEnum.Of(movement.Kind),
            movement.ItemId,
            movement.BranchId,
            movement.LotId,
            lot.LotNumber,
            lot.ExpiresOn,
            movement.Quantity,
            movement.BalanceAfter,
            movement.OccurredAt,
            movement.RecordedByStaffId,
            movement.Reference,
            movement.Note,
            movement.UnitCostSatang,
            movement.PurchaseOrderId,
            movement.PrescriptionId,
            movement.TransferId);
}

/// <summary>
/// The answer of <c>POST /staff/inventory/movements</c> and <c>POST /staff/inventory/adjustments</c>:
/// the ledger rows written, the item's stock afterwards at the branches they touched, and the
/// purchase order when goods were received against one.
/// </summary>
public sealed record StockChangeDto(
    InventoryItemDto Item,
    IReadOnlyList<StockMovementDto> Movements,
    PurchaseOrderDto? PurchaseOrder = null);
