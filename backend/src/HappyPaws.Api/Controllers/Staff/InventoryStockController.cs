using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Inventory;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Inventory.AdjustStock;
using HappyPaws.Application.Inventory.CreatePurchaseOrder;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Application.Inventory.RecordMovement;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The three writes of "Inventory", each under its own scope: moving stock
/// (<c>inventory:write</c>), correcting it after a count (<c>inventory:adjust</c>), and ordering it
/// (<c>inventory:purchase</c>).
/// </summary>
[ApiController]
[Route("staff/inventory")]
public sealed class InventoryStockController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/inventory/movements</c> — "รับเข้า จ่ายออก โอนสาขา · อ้างอิงเอกสารต้นทาง".
    /// <c>201</c> with the ledger rows written and the item's stock afterwards.
    /// <para>
    /// Requires <c>Idempotency-Key</c>: a receipt retried after a timeout must not put the same
    /// delivery on the shelf twice.
    /// </para>
    /// </summary>
    /// <param name="request">The movement.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("movements")]
    [Idempotent]
    [RequireScope(Scopes.Inventory.Write)]
    [ProducesResponseType<StockChangeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordMovement(
        [FromBody] RecordMovementRequest request,
        [FromServices] RecordMovementHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await handler.HandleAsync(request.ToCommand(), cancellationToken));

    /// <summary>
    /// <c>POST /staff/inventory/adjustments</c> — "ปรับยอดหลังนับสต๊อก · ต้องระบุเหตุผล". <c>201</c>
    /// with the adjustment row and the item's stock afterwards. Sets the lot to the count, so sending
    /// the same count twice changes nothing the second time.
    /// </summary>
    /// <param name="request">The count.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("adjustments")]
    [RequireScope(Scopes.Inventory.Adjust)]
    [ProducesResponseType<StockChangeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Adjust(
        [FromBody] AdjustStockRequest request,
        [FromServices] AdjustStockHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await handler.HandleAsync(request.ToCommand(), cancellationToken));

    /// <summary>
    /// <c>POST /staff/inventory/purchase-orders</c> — "สั่งซื้อจากซัพพลายเออร์ · รับของบางส่วนได้".
    /// <c>201</c> with the order. Goods arrive later as receipts naming it. Requires
    /// <c>Idempotency-Key</c>, as money the clinic commits to spend.
    /// </summary>
    /// <param name="request">The order.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("purchase-orders")]
    [Idempotent]
    [RequireScope(Scopes.Inventory.Purchase)]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreatePurchaseOrder(
        [FromBody] CreatePurchaseOrderRequest request,
        [FromServices] CreatePurchaseOrderHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await handler.HandleAsync(request.ToCommand(), cancellationToken));
}
