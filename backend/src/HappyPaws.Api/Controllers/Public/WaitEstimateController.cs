using HappyPaws.Api.Extensions;
using HappyPaws.Application.Queueing.WaitEstimate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// About how long a walk-in waits right now. <b>Not in the API reference</b>: see
/// <c>docs/contract-extensions.md § 37</c>.
/// </summary>
[ApiController]
[Route("queue/wait-estimate")]
[AllowAnonymous]
public sealed class WaitEstimateController : ControllerBase
{
    /// <summary>
    /// <c>GET /queue/wait-estimate?branch=</c>: tickets waiting, vets on shift, and a ten-minute
    /// range for the wait, or <c>open_now: false</c> with no range when the branch is closed. Counts
    /// only: no ticket, pet, owner or vet is named. Open to anyone and capped per address
    /// (<c>429</c>); the answer is reused for 30 seconds (<c>Cache-Control: public, max-age=30</c>).
    /// <c>branch</c> may be omitted when the clinic has one open branch; otherwise, and for a branch
    /// that is not open, <c>422</c> at <c>branch</c>.
    /// </summary>
    /// <param name="branch">One open branch.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [EnableRateLimiting(RateLimitingExtensions.WaitEstimatePolicy)]
    [ProducesResponseType<WaitEstimateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<WaitEstimateDto>> Get(
        [FromQuery] string? branch,
        [FromServices] GetWaitEstimateHandler handler,
        CancellationToken cancellationToken)
    {
        var estimate = await handler.HandleAsync(new GetWaitEstimateQuery(branch), cancellationToken);

        Response.Headers.CacheControl = $"public, max-age={WaitEstimateRules.CacheSeconds}";

        return Ok(estimate);
    }
}
