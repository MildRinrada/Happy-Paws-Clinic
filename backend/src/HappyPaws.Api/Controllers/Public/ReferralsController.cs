using HappyPaws.Api.Extensions;
using HappyPaws.Application.Referrals;
using HappyPaws.Application.Referrals.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// Whether a referral code can be used. <b>Not in the API reference</b>: see
/// <c>docs/contract-extensions.md § 39</c>.
/// </summary>
[ApiController]
[Route("referrals")]
[AllowAnonymous]
public sealed class ReferralsController : ControllerBase
{
    /// <summary>
    /// <c>GET /referrals/{code}</c>: <c>{ "valid": true }</c> or <c>{ "valid": false }</c>, always
    /// <c>200</c>. Never names the customer the code belongs to or anything about them, and answers
    /// the same for an unknown code, a malformed one, and any code while the programme is off.
    /// Capped per address (<c>429</c>).
    /// </summary>
    /// <param name="code">As shared, any case, spaces and dashes ignored.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{code}")]
    [EnableRateLimiting(RateLimitingExtensions.ReferralCheckPolicy)]
    [ProducesResponseType<ReferralCodeCheckDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ReferralCodeCheckDto>> Check(
        string code,
        [FromServices] CheckReferralCodeHandler handler,
        CancellationToken cancellationToken)
    {
        var check = await handler.HandleAsync(code, cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Ok(check);
    }
}
