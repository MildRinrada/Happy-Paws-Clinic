using HappyPaws.Application.Common;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.Dtos;

/// <summary>
/// A purchase order — the answer of <c>POST /staff/inventory/purchase-orders</c>, and of a receipt
/// against one.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>status</c>: <c>ordered</c>, <c>partially_received</c>, <c>received</c> or <c>cancelled</c>.</item>
/// <item><c>total_cost_satang</c>: What the order costs at the agreed unit costs, as ordered.</item>
/// <item><c>received_cost_satang</c>: The same for what has arrived.</item>
/// <item><c>cancelled_at</c>, <c>cancelled_by_staff_id</c>, <c>cancel_reason</c>: set once the rest
/// was called off. These, <c>received_cost_satang</c> and the <c>cancelled</c> status are
/// <c>docs/contract-extensions.md § 25</c>, not in the API reference.</item>
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
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    long ReceivedCostSatang = 0,
    DateTimeOffset? CancelledAt = null,
    string? CancelledByStaffId = null,
    string? CancelReason = null)
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
                    order.IsCancelled ? 0 : line.Outstanding,
                    line.UnitCostSatang,
                    line.LineTotal.Satang,
                    order.IsCancelled ? line.Outstanding : 0)),
            ],
            order.Lines.Sum(line => line.UnitCostSatang * line.ReceivedQuantity),
            order.CancelledAt,
            order.CancelledByStaffId,
            order.CancelReason);
}

/// <remarks>
/// <list type="bullet">
/// <item><c>outstanding</c>: Ordered and not yet received; zero once the order is cancelled.</item>
/// <item><c>cancelled_quantity</c>: What was still outstanding when the order was cancelled (§ 25).</item>
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
    long LineTotalSatang,
    int CancelledQuantity = 0);
