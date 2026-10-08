using HappyPaws.Application.Common;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.Dtos;

/// <summary>
/// A purchase order — the answer of <c>POST /staff/inventory/purchase-orders</c>, and of a receipt
/// against one.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>status</c>: <c>ordered</c>, <c>partially_received</c> or <c>received</c>.</item>
/// <item><c>total_cost_satang</c>: What the order costs at the agreed unit costs.</item>
/// </list>
/// </remarks>
public sealed record PurchaseOrderDto(
    string Id,
    uint Version,
    string Number,
    string BranchId,
    string SupplierName,
    string Status,
    DateOnly? ExpectedOn,
    string? Note,
    DateTimeOffset OrderedAt,
    string OrderedByStaffId,
    long TotalCostSatang,
    IReadOnlyList<PurchaseOrderLineDto> Lines)
{
    public static PurchaseOrderDto From(PurchaseOrder order, IReadOnlyDictionary<string, InventoryItem> items) =>
        new(
            order.Id,
            order.Version,
            order.Number,
            order.BranchId,
            order.SupplierName,
            WireEnum.Of(order.Status),
            order.ExpectedOn,
            order.Note,
            order.OrderedAt,
            order.OrderedByStaffId,
            order.TotalCost.Satang,
            [
                .. order.Lines.Select(line => new PurchaseOrderLineDto(
                    line.Id,
                    line.ItemId,
                    items.TryGetValue(line.ItemId, out var item) ? item.Name : line.ItemId,
                    items.TryGetValue(line.ItemId, out var unit) ? unit.Unit : string.Empty,
                    line.Quantity,
                    line.ReceivedQuantity,
                    line.Outstanding,
                    line.UnitCostSatang,
                    line.LineTotal.Satang)),
            ]);
}

/// <remarks>
/// <list type="bullet">
/// <item><c>outstanding</c>: Ordered and not yet received.</item>
/// </list>
/// </remarks>
public sealed record PurchaseOrderLineDto(
    string Id,
    string ItemId,
    string ItemName,
    string Unit,
    int Quantity,
    int ReceivedQuantity,
    int Outstanding,
    long UnitCostSatang,
    long LineTotalSatang);
