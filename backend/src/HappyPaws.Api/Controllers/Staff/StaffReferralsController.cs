using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Referrals;
using HappyPaws.Application.Referrals.Staff;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Who referred whom. <b>Not in the API reference</b>: see <c>docs/contract-extensions.md § 39</c>.
/// On <c>crm:read</c>, beside the points ledger the rewards land in.
/// </summary>
[ApiController]
[Route("staff/referrals")]
public sealed class StaffReferralsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/referrals?customer=&amp;limit=&amp;cursor=</c>: referrals newest first, all of them
    /// or those where the customer is either side, with both names, the status
    /// (<c>pending</c>, <c>rewarded</c>, <c>rejected</c> with its reason), the points each side got,
    /// and when.
    /// </summary>
    /// <param name="customer">A customer id, either side.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Crm.Read)]
    [ProducesResponseType<CursorPageResponse<StaffReferralDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CursorPageResponse<StaffReferralDto>>> List(
        [FromQuery] string? customer,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListReferralsHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListReferralsQuery(customer, limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Ok(CursorPageResponse<StaffReferralDto>.From(page));
    }
}
