using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Crm;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Crm.CreateCoupon;
using HappyPaws.Application.Crm.DeactivateCoupon;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Crm.ListCoupons;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Discount codes. Only <c>POST</c> is in the contract; the list and switching a coupon off are
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
[ApiController]
[Route("staff/coupons")]
public sealed class CouponsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/coupons</c> — "โค้ดส่วนลด · จำกัดบริการ จำนวน และช่วงเวลา". <c>201</c> with the
    /// coupon. Redeemed at the cash desk with <c>coupon_code</c> on
    /// <c>POST /staff/invoices/{id}/discounts</c>. <c>409 coupon_code_taken</c> when the code is in use.
    /// </summary>
    /// <param name="request">The code, the discount and its limits.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Crm.Manage)]
    [ProducesResponseType<CouponDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCouponRequest request,
        [FromServices] CreateCouponHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await handler.HandleAsync(request.ToCommand(), cancellationToken));

    /// <summary>
    /// <c>GET /staff/coupons?status=&amp;branch=&amp;q=&amp;limit=&amp;cursor=</c> — the coupons issued, newest
    /// first, each with <c>redeemed_count</c>, <c>remaining</c> and <c>status</c> (<c>active</c>,
    /// <c>scheduled</c>, <c>used_up</c>, <c>expired</c>, <c>deactivated</c>). Branch-bound staff see their
    /// branches' coupons and those for every branch. Not in the API reference (§ 25).
    /// </summary>
    /// <param name="status">One status.</param>
    /// <param name="branch">Coupons a bill at this branch could take.</param>
    /// <param name="q">Part of the code or the name.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Crm.Read)]
    [ProducesResponseType<CursorPageResponse<CouponDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CursorPageResponse<CouponDto>>> List(
        [FromQuery] string? status,
        [FromQuery] string? branch,
        [FromQuery] string? q,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListCouponsHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListCouponsQuery(status, branch, q, limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        return Ok(CursorPageResponse<CouponDto>.From(page));
    }

    /// <summary>
    /// <c>POST /staff/coupons/{id}/deactivate</c> — switches the coupon off before its last day. Uses
    /// already on bills stand; the next redemption is <c>409 coupon_deactivated</c>. Twice is
    /// <c>409 state_conflict</c>. Audited as <c>coupon.deactivated</c>. Not in the API reference (§ 25).
    /// </summary>
    /// <param name="id">The coupon.</param>
    /// <param name="request">An optional reason.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/deactivate")]
    [RequireScope(Scopes.Crm.Manage)]
    [ProducesResponseType<CouponDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<CouponDto> Deactivate(
        string id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeactivateCouponRequest? request,
        [FromServices] DeactivateCouponHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new DeactivateCouponCommand(id, request?.Reason), cancellationToken);
}
