using FluentValidation;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.ListPurchaseOrders;

/// <summary>
/// <c>GET /staff/inventory/purchase-orders?status=&amp;branch=&amp;supplier=&amp;limit=&amp;cursor=</c>. Not in
/// the API reference: see <c>docs/contract-extensions.md § 25</c>.
/// </summary>
public sealed record ListPurchaseOrdersQuery(
    string? Status = null,
    string? BranchId = null,
    string? Supplier = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

public sealed class ListPurchaseOrdersValidator : AbstractValidator<ListPurchaseOrdersQuery>
{
    public ListPurchaseOrdersValidator()
    {
        RuleFor(query => query.Status)
            .Must(status => WireEnum.TryParse<PurchaseOrderStatus>(status, out _))
            .WithMessage($"status ต้องเป็น {WireEnum.Names<PurchaseOrderStatus>()}")
            .When(query => !string.IsNullOrEmpty(query.Status));

        RuleFor(query => query.BranchId).MaximumLength(40);
        RuleFor(query => query.Supplier).MaximumLength(PurchaseOrder.SupplierMax);
    }
}

/// <summary>
/// The purchase orders, newest first, each with its lines, what has arrived and what it cost.
/// <para>
/// Stock and its orders belong to a branch: <c>branch</c> names one the caller works at (another is
/// <c>403</c>), and without it the list covers every branch the caller may see. On
/// <c>inventory:purchase</c>, the scope that raises orders, because the list shows unit costs, which
/// the stock list does not.
/// </para>
/// </summary>
public sealed class ListPurchaseOrdersHandler(
    IPurchaseOrderRepository orders,
    IInventoryItemRepository items,
    InventoryBranches branches,
    RequestValidator validator)
{
    public async Task<CursorPage<PurchaseOrderDto>> HandleAsync(
        ListPurchaseOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(query, cancellationToken);

        var scope = await branches.ForReadAsync(query.BranchId, cancellationToken);
        PurchaseOrderStatus? status = WireEnum.TryParse<PurchaseOrderStatus>(query.Status, out var parsed) ? parsed : null;

        var page = await orders.ListAsync(
            new PurchaseOrderFilter(scope, status, query.Supplier?.Trim() is { Length: > 0 } supplier ? supplier : null),
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        var named = (await items.FindManyAsync(
                [.. page.Data.SelectMany(order => order.Lines).Select(line => line.ItemId).Distinct()],
                cancellationToken))
            .ToDictionary(item => item.Id);

        return new CursorPage<PurchaseOrderDto>([.. page.Data.Select(order => PurchaseOrderDto.From(order, named))], page.NextCursor);
    }
}
