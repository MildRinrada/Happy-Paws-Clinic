using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory;

/// <summary>A purchase-order line that is still waiting for goods, for the stock list's "on order".</summary>
public sealed record OpenOrderLine(
    string PurchaseOrderId,
    string Number,
    string BranchId,
    string SupplierName,
    string ItemId,
    int Outstanding,
    DateOnly? ExpectedOn);

/// <summary>Persistence port for orders to suppliers.</summary>
public interface IPurchaseOrderRepository
{
    void Add(PurchaseOrder order);

    /// <summary>
    /// The order with its lines, row-locked until the transaction ends. Two receipts against one
    /// order, even of different items, therefore take turns, and neither can push a line past what
    /// was ordered.
    /// </summary>
    Task<PurchaseOrder?> FindForUpdateAsync(string purchaseOrderId, CancellationToken cancellationToken = default);

    /// <summary>e.g. <c>PO-202610-0007</c>. Inside the caller's transaction, like an invoice number.</summary>
    Task<string> NextNumberAsync(DateTimeOffset orderedAt, CancellationToken cancellationToken = default);

    /// <summary>Lines with goods still to come, narrowed by item and branch when given.</summary>
    Task<IReadOnlyList<OpenOrderLine>> OpenLinesAsync(
        IReadOnlyCollection<string>? itemIds,
        IReadOnlyCollection<string>? branchIds,
        CancellationToken cancellationToken = default);
}
