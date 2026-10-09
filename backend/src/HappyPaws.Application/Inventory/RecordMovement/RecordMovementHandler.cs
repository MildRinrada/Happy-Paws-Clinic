using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.RecordMovement;

/// <summary>
/// Records goods arriving, leaving, or moving between branches.
/// <para>
/// <b>Under the stock lock.</b> Every path locks the item at the branches it touches before reading
/// a lot, then writes the lot balance and the ledger row together; see <see cref="IStockLedger"/>.
/// </para>
/// <para>
/// <b>Receiving against an order</b> is how "รับของบางส่วนได้" works: the receipt names the order,
/// the order's line for the item counts it, and more than is outstanding is refused with
/// <c>409 over_receipt</c>. The order must be for the branch the goods arrived at.
/// </para>
/// <para>
/// <b>Not audited.</b> The ledger row is the record: who, when, how many, which lot, and against
/// which document. Adjustments are audited, because an adjustment has no document behind it.
/// </para>
/// </summary>
public sealed class RecordMovementHandler(
    InventoryItemResolver resolver,
    IInventoryItemRepository items,
    IStockLedger ledger,
    IPurchaseOrderRepository orders,
    InventoryBranches branches,
    BranchGuard branchGuard,
    StockView view,
    ClinicDays clinicDays,
    ICurrentUser currentUser,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<StockChangeDto> HandleAsync(RecordMovementCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var branchId = await branches.ForWriteAsync(command.BranchId, "branch_id", cancellationToken);
        var type = command.ParsedType;
        var today = clinicDays.Today;
        string? toBranchId = null;

        if (type == MovementType.Transfer)
        {
            toBranchId = command.ToBranchId!.Trim();

            if (toBranchId == branchId)
            {
                throw Invalid("to_branch_id", "สาขาปลายทางต้องไม่ใช่สาขาเดียวกับต้นทาง");
            }

            await branches.EnsureOpenAsync(toBranchId, "to_branch_id", cancellationToken);
        }

        if (type == MovementType.Receive && command.ExpiresOn is { } expires && expires < today)
        {
            throw Invalid("expires_on", "ล็อตนี้หมดอายุแล้ว รับเข้าไม่ได้");
        }

        var written = new List<(StockMovement Movement, StockLot Lot)>();
        InventoryItem? item = null;
        PurchaseOrder? order = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            written.Clear();
            var now = clock.UtcNow;

            item = type == MovementType.Receive
                ? await resolver.ResolveAsync(command.ItemId, command.NewItem, staffId, token)
                : await items.FindAsync(command.ItemId!.Trim(), token) ?? throw new NotFoundException("inventory_item", command.ItemId!);

            switch (type)
            {
                case MovementType.Receive:
                    order = await ReceiveAsync(command, item, branchId, staffId, now, written, token);
                    break;
                case MovementType.Issue:
                    await IssueAsync(command, item, branchId, staffId, now, today, written, token);
                    break;
                default:
                    await TransferAsync(command, item, branchId, toBranchId!, staffId, now, today, written, token);
                    break;
            }
        }, cancellationToken);

        IReadOnlyList<string> touched = toBranchId is null
            ? [branchId]
            : [.. new[] { branchId, toBranchId }.Where(branchGuard.CanAccess)];

        var stock = await view.ForAsync(item!.Id, touched, cancellationToken);
        var movements = written
            .Where(row => branchGuard.CanAccess(row.Movement.BranchId))
            .Select(row => StockMovementDto.From(row.Movement, row.Lot))
            .ToList();

        PurchaseOrderDto? orderDto = null;

        if (order is not null)
        {
            var named = await items.FindManyAsync([.. order.Lines.Select(line => line.ItemId)], cancellationToken);
            orderDto = PurchaseOrderDto.From(order, named.ToDictionary(entry => entry.Id));
        }

        return new StockChangeDto(stock, movements, orderDto);
    }

    private async Task<PurchaseOrder?> ReceiveAsync(
        RecordMovementCommand command,
        InventoryItem item,
        string branchId,
        string staffId,
        DateTimeOffset now,
        List<(StockMovement, StockLot)> written,
        CancellationToken cancellationToken)
    {
        var quantity = command.Quantity!.Value;
        var unitCost = command.UnitCostSatang;
        PurchaseOrder? order = null;

        await ledger.LockAsync([new StockKey(item.Id, branchId)], cancellationToken);

        if (command.PurchaseOrderId is { Length: > 0 } orderId)
        {
            order = await orders.FindForUpdateAsync(orderId.Trim(), cancellationToken)
                ?? throw new NotFoundException("purchase_order", orderId);

            if (order.BranchId != branchId)
            {
                throw Invalid("purchase_order_id", "ใบสั่งซื้อนี้เป็นของสาขาอื่น");
            }

            var line = order.Receive(item.Id, quantity);
            unitCost ??= line.UnitCostSatang;
        }

        var lots = await ledger.LotsForUpdateAsync(new StockKey(item.Id, branchId), cancellationToken);
        var lot = lots.FirstOrDefault(candidate => candidate.Matches(command.LotNumber, command.ExpiresOn));

        if (lot is null)
        {
            lot = StockLot.Open(item.Id, branchId, command.LotNumber, command.ExpiresOn, now);
            ledger.AddLot(lot);
        }

        var movement = StockMovement.Receive(
            lot, quantity, staffId, now, unitCost, order?.Id, command.Reference ?? order?.Number, command.Note);

        ledger.Append(movement);
        written.Add((movement, lot));

        return order;
    }

    private async Task IssueAsync(
        RecordMovementCommand command,
        InventoryItem item,
        string branchId,
        string staffId,
        DateTimeOffset now,
        DateOnly today,
        List<(StockMovement, StockLot)> written,
        CancellationToken cancellationToken)
    {
        var key = new StockKey(item.Id, branchId);
        await ledger.LockAsync([key], cancellationToken);

        var lots = await ledger.LotsForUpdateAsync(key, cancellationToken);
        var before = StockAllocation.Available(lots, today);

        foreach (var take in StockTakes.Choose(lots, command.LotId, command.Quantity!.Value, today))
        {
            var movement = StockMovement.Issue(take.Lot, take.Quantity, staffId, now, command.Reference, command.Note);
            ledger.Append(movement);
            written.Add((movement, take.Lot));
        }

        LowStockWebhook.Report(webhooks, item, branchId, before, StockAllocation.Available(lots, today));
    }

    private async Task TransferAsync(
        RecordMovementCommand command,
        InventoryItem item,
        string fromBranchId,
        string toBranchId,
        string staffId,
        DateTimeOffset now,
        DateOnly today,
        List<(StockMovement, StockLot)> written,
        CancellationToken cancellationToken)
    {
        var from = new StockKey(item.Id, fromBranchId);
        var to = new StockKey(item.Id, toBranchId);
        await ledger.LockAsync([from, to], cancellationToken);

        var source = await ledger.LotsForUpdateAsync(from, cancellationToken);
        var destination = (await ledger.LotsForUpdateAsync(to, cancellationToken)).ToList();
        var transferId = EntityId.New(EntityId.StockTransfer);
        var sourceBefore = StockAllocation.Available(source, today);

        foreach (var take in StockTakes.Choose(source, command.LotId, command.Quantity!.Value, today))
        {
            var outgoing = StockMovement.Transfer(
                take.Lot, take.Quantity, outgoing: true, transferId, staffId, now, command.Reference, command.Note);

            // The receiving branch gets the same lot number and expiry: a lot is still that lot on
            // another shelf, and a recall of it has to find both halves.
            var landing = destination.FirstOrDefault(lot => lot.Matches(take.Lot.LotNumber, take.Lot.ExpiresOn));

            if (landing is null)
            {
                landing = StockLot.Open(item.Id, toBranchId, take.Lot.LotNumber, take.Lot.ExpiresOn, now);
                ledger.AddLot(landing);
                destination.Add(landing);
            }

            var incoming = StockMovement.Transfer(
                landing, take.Quantity, outgoing: false, transferId, staffId, now, command.Reference, command.Note);

            ledger.Append(outgoing);
            ledger.Append(incoming);
            written.Add((outgoing, take.Lot));
            written.Add((incoming, landing));
        }

        LowStockWebhook.Report(webhooks, item, fromBranchId, sourceBefore, StockAllocation.Available(source, today));
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
