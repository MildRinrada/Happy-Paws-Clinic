using HappyPaws.Api.Contracts.Queueing;
using HappyPaws.Api.Extensions;
using HappyPaws.Application.Queueing.Displays;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Display;

/// <summary>
/// The TV's half of setting up a waiting-room screen. <b>Not in the API reference</b>: see
/// <c>docs/contract-extensions.md § 30</c>.
/// </summary>
[ApiController]
[Route("queue/displays/claim")]
[AllowAnonymous]
public sealed class DisplayClaimController : ControllerBase
{
    /// <summary>
    /// <c>POST /queue/displays/claim</c> — trades the setup code from <c>POST /staff/queue/displays</c>
    /// for the screen's token, once. Open to anyone, because the TV has no credential yet, and capped
    /// per address (<c>429</c>). A wrong, used, expired or revoked code is the same <c>422</c> at
    /// <c>code</c>. The answer is the only place the token ever appears;
    /// <c>Cache-Control: no-store</c>.
    /// </summary>
    /// <param name="request">The code, with or without its dash.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.DisplayClaimPolicy)]
    [ProducesResponseType<ClaimedQueueDisplayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ClaimedQueueDisplayDto>> Claim(
        [FromBody] ClaimQueueDisplayRequest request,
        [FromServices] ClaimQueueDisplayHandler handler,
        CancellationToken cancellationToken)
    {
        var claimed = await handler.HandleAsync(new ClaimQueueDisplayCommand(request.Code), cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Ok(claimed);
    }
}
