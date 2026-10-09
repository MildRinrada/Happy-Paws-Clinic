using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Inventory;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Inventory.CancelPurchaseOrder;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Application.Inventory.ListPurchaseOrders;
using HappyPaws.Application.Inventory.UpdateItem;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Managing what "Inventory" created: editing and retiring catalogue items, and reading and
/// cancelling purchase orders. None of the three is in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
[ApiController]
[Route("staff/inventory")]
public sealed class InventoryManagementController : ControllerBase
{
    /// <summary>
    /// <c>PATCH /staff/inventory/items/{id}</c> — name, unit, selling price, reorder level, and
    /// <c>active</c> to retire or bring back. Fields left out are unchanged. The price applies to
    /// dispenses from now on. A name another item holds is <c>409 inventory_item_exists</c>.
    /// <c>If-Match</c> honoured. Answers the item with its stock and an <c>ETag</c>.
    /// </summary>
    /// <param name="id">The item.</param>
    /// <param name="request">What to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("items/{id}")]
    [RequireScope(Scopes.Inventory.Write)]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InventoryItemDto>> UpdateItem(
        string id,
        [FromBody] UpdateItemRequest request,
        [FromServices] UpdateItemHandler handler,
        CancellationToken cancellationToken)
    {
        var item = await handler.HandleAsync(
            new UpdateItemCommand(
                id,
                request.Name,
                request.Unit,
                request.UnitPriceSatang,
                request.ReorderLevel,
                request.Active,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(item.Version);

        return Ok(item);
    }

    /// <summary>
    /// <c>GET /staff/inventory/purchase-orders?status=&amp;branch=&amp;supplier=&amp;limit=&amp;cursor=</c> —
    /// orders with their lines, newest first, over the branches the caller may see. On
    /// <c>inventory:purchase</c>, because it shows unit costs.
    /// </summary>
    /// <param name="status"><c>ordered</c>, <c>partially_received</c>, <c>received</c> or <c>cancelled</c>.</param>
    /// <param name="branch">One branch.</param>
    /// <param name="supplier">Part of the supplier's name.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("purchase-orders")]
    [RequireScope(Scopes.Inventory.Purchase)]
    [ProducesResponseType<CursorPageResponse<PurchaseOrderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CursorPageResponse<PurchaseOrderDto>>> ListPurchaseOrders(
        [FromQuery] string? status,
        [FromQuery] string? branch,
        [FromQuery] string? supplier,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListPurchaseOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListPurchaseOrdersQuery(status, branch, supplier, limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        return Ok(CursorPageResponse<PurchaseOrderDto>.From(page));
    }

    /// <summary>
    /// <c>POST /staff/inventory/purchase-orders/{id}/cancel</c> — calls off what is still
    /// outstanding. Received lines and their stock stay. A fully received or already cancelled order
    /// is <c>409 state_conflict</c>. <c>If-Match</c> honoured. Audited as
    /// <c>inventory.purchase_order_cancelled</c>.
    /// </summary>
    /// <param name="id">The order.</param>
    /// <param name="request">An optional reason.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("purchase-orders/{id}/cancel")]
    [RequireScope(Scopes.Inventory.Purchase)]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderDto>> CancelPurchaseOrder(
        string id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelPurchaseOrderRequest? request,
        [FromServices] CancelPurchaseOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var order = await handler.HandleAsync(
            new CancelPurchaseOrderCommand(id, request?.Reason, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(order.Version);

        return Ok(order);
    }
}
