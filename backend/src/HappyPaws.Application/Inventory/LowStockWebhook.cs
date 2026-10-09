using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory;

/// <summary>
/// Queues <c>inventory.low_stock</c> when a change takes an item at a branch from above its reorder
/// level to at or below it. Once per crossing: the dispenses after that, while it stays low, send
/// nothing, and a receipt that lifts it back above the level arms it again.
/// <para>
/// Every path that takes stock away calls this, inside its own transaction, with the usable units
/// before and after: dispensing, issuing, transferring out, and a count that comes in lower.
/// </para>
/// </summary>
public static class LowStockWebhook
{
    public static void Report(IWebhookOutbox webhooks, InventoryItem item, string branchId, int before, int after)
    {
        if (!item.IsRetired
            && StockLevels.StatusOf(before, item.ReorderLevel) == StockStatus.Ok
            && StockLevels.StatusOf(after, item.ReorderLevel) != StockStatus.Ok)
        {
            webhooks.Enqueue(
                WebhookEventTypes.InventoryLowStock,
                new WebhookPayloads.LowStockData(item.Id, branchId, Math.Max(0, after), item.ReorderLevel));
        }
    }
}
