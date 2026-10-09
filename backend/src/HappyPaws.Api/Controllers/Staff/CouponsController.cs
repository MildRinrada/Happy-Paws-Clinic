using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Crm;
using HappyPaws.Application.Crm.CreateCoupon;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Discount codes.</summary>
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
}
