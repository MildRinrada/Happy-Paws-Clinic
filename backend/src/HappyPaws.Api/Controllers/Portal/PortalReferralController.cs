using HappyPaws.Api.Authorization;
using HappyPaws.Application.Referrals;
using HappyPaws.Application.Referrals.Portal;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// The customer's own referral code. <b>Not in the API reference</b>: see
/// <c>docs/contract-extensions.md § 39</c>.
/// </summary>
[ApiController]
[Route("portal/referral")]
[RequireScope(Scopes.Portal)]
public sealed class PortalReferralController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/referral</c>: the code (made on the first read), the link to share, and counts
    /// of friends invited, rewarded and pending, the points earned, and this month against the cap.
    /// Counts only: no friend is named.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<MyReferralDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyReferralDto>> Get(
        [FromServices] GetMyReferralHandler handler,
        CancellationToken cancellationToken)
    {
        var referral = await handler.HandleAsync(cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Ok(referral);
    }
}
