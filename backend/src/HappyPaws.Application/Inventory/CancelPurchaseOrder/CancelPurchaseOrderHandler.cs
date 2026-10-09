using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.CancelPurchaseOrder;

/// <summary>
/// <c>POST /staff/inventory/purchase-orders/{id}/cancel</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
public sealed record CancelPurchaseOrderCommand(string PurchaseOrderId, string? Reason = null, uint? IfMatchVersion = null);

public sealed class CancelPurchaseOrderValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderValidator()
    {
        RuleFor(command => command.Reason).MaximumLength(PurchaseOrder.NoteMax);
    }
}

/// <summary>
/// Calls off what is still outstanding on an order.
/// <para>
/// <b>The remainder, not the order.</b> Allowed while anything is outstanding, whether nothing or
/// part has arrived. Lines keep what they received and those receipts stay in stock; what was still
/// to come leaves "on order" and can no longer be received against the order (<c>409
/// state_conflict</c>). An order that has fully arrived has nothing to cancel and is
/// <c>409 state_conflict</c>, as is cancelling twice.
/// </para>
/// <para>
/// <b>Under the order's row lock</b>, the lock every receipt against it takes, so a receipt and a
/// cancellation at the same moment take turns: the receipt either lands first and counts, or finds
/// the order cancelled. <c>If-Match</c> is honoured when sent. Another branch's order is <c>403</c>.
/// Audited with what was called off.
/// </para>
/// </summary>
public sealed class CancelPurchaseOrderHandler(
    IPurchaseOrderRepository orders,
    IInventoryItemRepository items,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<PurchaseOrderDto> HandleAsync(CancelPurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        PurchaseOrder? cancelled = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var order = await orders.FindForUpdateAsync(command.PurchaseOrderId, token)
                ?? throw new NotFoundException("purchase_order", command.PurchaseOrderId);

            branchGuard.EnsureCanAccess(order.BranchId);
            ConcurrencyGuard.Ensure(order, command.IfMatchVersion);

            var outstanding = order.Lines.Where(line => line.Outstanding > 0).ToList();
            var statusBefore = order.Status;

            order.Cancel(staffId, command.Reason, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.PurchaseOrderCancelled,
                AuditResources.PurchaseOrder,
                order.Id,
                BranchId: order.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["number"] = order.Number,
                    ["supplier_name"] = order.SupplierName,
                    ["status_before"] = statusBefore.ToString(),
                    ["reason"] = order.CancelReason,
                    ["cancelled_lines"] = outstanding.Count,
                    ["cancelled_cost_satang"] = outstanding.Sum(line => line.UnitCostSatang * line.Outstanding),
                }));

            cancelled = order;
        }, cancellationToken);

        var named = (await items.FindManyAsync([.. cancelled!.Lines.Select(line => line.ItemId)], cancellationToken))
            .ToDictionary(item => item.Id);

        return PurchaseOrderDto.From(cancelled, named);
    }
}
