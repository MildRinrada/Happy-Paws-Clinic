using HappyPaws.Api.Contracts.Reviews;
using HappyPaws.Api.Extensions;
using HappyPaws.Application.Reviews;
using HappyPaws.Application.Reviews.Invitations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// Reviewing a visit through a one-time invitation link, for customers without a portal account.
/// <b>Contract extension:</b> see <c>docs/contract-extensions.md § 36</c>. Public: the token in the
/// path is the credential, good for one review of one visit for 30 days, and nothing else.
/// </summary>
[ApiController]
[Route("reviews/invitations")]
[AllowAnonymous]
public sealed class ReviewInvitationsController : ControllerBase
{
    /// <summary>
    /// <c>GET /reviews/invitations/{token}</c>: the visit's day, service and pet name, and whether the
    /// link is <c>usable</c>, <c>used</c> or <c>expired</c>. An unknown token is <c>404</c>.
    /// </summary>
    /// <param name="token">The one-time token from the message.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{token}")]
    [ProducesResponseType<ReviewInvitationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewInvitationDto>> Get(
        string token,
        [FromServices] GetReviewInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        var invitation = await handler.HandleAsync(token, cancellationToken);
        Response.Headers.CacheControl = "no-store";

        return Ok(invitation);
    }

    /// <summary>
    /// <c>POST /reviews/invitations/{token}</c>: writes the review, pending until staff decide.
    /// <c>201</c>. Used is <c>409 review_already_submitted</c>, expired
    /// <c>409 review_invitation_expired</c>, unknown <c>404</c>. Limited per address.
    /// </summary>
    /// <param name="token">The one-time token from the message.</param>
    /// <param name="request">The score, the words and the name choice.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{token}")]
    [EnableRateLimiting(RateLimitingExtensions.ReviewSubmitPolicy)]
    [ProducesResponseType<SubmittedReviewDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SubmittedReviewDto>> Submit(
        string token,
        [FromBody] SubmitInvitationReviewRequest request,
        [FromServices] SubmitInvitationReviewHandler handler,
        CancellationToken cancellationToken)
    {
        var review = await handler.HandleAsync(
            new SubmitInvitationReviewCommand(token, request.Rating, request.Text, request.DisplayName),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, review);
    }
}
