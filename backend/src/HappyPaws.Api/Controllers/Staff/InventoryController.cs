using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Application.Inventory.ListAlerts;
using HappyPaws.Application.Inventory.ListItems;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The two reads of "Inventory", under <c>inventory:read</c>.</summary>
[ApiController]
[Route("staff/inventory")]
public sealed class InventoryController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/inventory/items</c> — "ยาและเวชภัณฑ์ · คงเหลือรายสาขา". The whole catalogue by name,
    /// in <c>{ "data": [...] }</c>, each item with its stock at every branch the caller may see.
    /// </summary>
    /// <param name="q">Part of the item's name.</param>
    /// <param name="branch">One branch.</param>
    /// <param name="status"><c>ok</c>, <c>low</c> or <c>out</c>.</param>
    /// <param name="active">
    /// <c>true</c> for items in use, <c>false</c> for retired ones; both when absent. Not in the API
    /// reference: <c>docs/contract-extensions.md § 25</c>.
    /// </param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("items")]
    [RequireScope(Scopes.Inventory.Read)]
    [ProducesResponseType<ListResponse<InventoryItemDto>>(StatusCodes.Status200OK)]
    public async Task<ListResponse<InventoryItemDto>> Items(
        [FromQuery] string? q,
        [FromQuery] string? branch,
        [FromQuery] string? status,
        [FromQuery] bool? active,
        [FromServices] ListItemsHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(new ListItemsQuery(q, branch, status, active), cancellationToken));

    /// <summary>
    /// <c>GET /staff/inventory/alerts</c> — "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน", worst first.
    /// </summary>
    /// <param name="branch">One branch.</param>
    /// <param name="withinDays">How far ahead "ใกล้หมดอายุ" looks: 1 to 365, 90 when absent.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("alerts")]
    [RequireScope(Scopes.Inventory.Read)]
    [ProducesResponseType<ListResponse<InventoryAlertDto>>(StatusCodes.Status200OK)]
    public async Task<ListResponse<InventoryAlertDto>> Alerts(
        [FromQuery] string? branch,
        [FromQuery(Name = "within_days")] int? withinDays,
        [FromServices] ListAlertsHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(new ListAlertsQuery(branch, withinDays), cancellationToken));
}
