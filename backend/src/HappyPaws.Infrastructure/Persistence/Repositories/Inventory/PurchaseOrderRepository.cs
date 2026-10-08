using System.Globalization;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Inventory;
using HappyPaws.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Inventory;

internal sealed class PurchaseOrderRepository(HappyPawsDbContext db, IClock clock) : IPurchaseOrderRepository
{
    public void Add(PurchaseOrder order) => db.Set<PurchaseOrder>().Add(order);

    public async Task<PurchaseOrder?> FindForUpdateAsync(string purchaseOrderId, CancellationToken cancellationToken = default)
    {
        // Lock the order row first, then load it with its lines through the ordinary query: the
        // lock is what makes two receipts against one order take turns.
        var locked = await db.Database
            .SqlQuery<string>($"SELECT id AS \"Value\" FROM purchase_orders WHERE id = {purchaseOrderId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        if (locked.Count == 0)
        {
            return null;
        }

        return await db.Set<PurchaseOrder>()
            .Include(order => order.Lines)
            .FirstAsync(order => order.Id == purchaseOrderId, cancellationToken);
    }

    public async Task<string> NextNumberAsync(DateTimeOffset orderedAt, CancellationToken cancellationToken = default)
    {
        var period = TimeZoneInfo.ConvertTime(orderedAt, clock.ClinicTimeZone).ToString("yyyyMM", CultureInfo.InvariantCulture);
        var prefix = $"PO-{period}-";
        var next = await DocumentSequence.NextAsync(db, "purchase_orders", "number", prefix, segment: 3, cancellationToken);

        return $"{prefix}{next:D4}";
    }

    public async Task<IReadOnlyList<OpenOrderLine>> OpenLinesAsync(
        IReadOnlyCollection<string>? itemIds,
        IReadOnlyCollection<string>? branchIds,
        CancellationToken cancellationToken = default)
    {
        var orders = db.Set<PurchaseOrder>().AsNoTracking()
            .Where(order => order.Status != PurchaseOrderStatus.Received);

        if (branchIds is not null)
        {
            var branches = branchIds.ToArray();
            orders = orders.Where(order => branches.Contains(order.BranchId));
        }

        var items = itemIds?.ToArray();

        var rows = await orders
            .SelectMany(order => order.Lines
                .Where(line => line.ReceivedQuantity < line.Quantity && (items == null || items.Contains(line.ItemId)))
                .Select(line => new
                {
                    order.Id,
                    order.Number,
                    order.BranchId,
                    order.SupplierName,
                    line.ItemId,
                    Outstanding = line.Quantity - line.ReceivedQuantity,
                    order.ExpectedOn,
                }))
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new OpenOrderLine(row.Id, row.Number, row.BranchId, row.SupplierName, row.ItemId, row.Outstanding, row.ExpectedOn)),
        ];
    }
}
