using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Reviews;
using HappyPaws.Application.Reviews;
using HappyPaws.Application.Reviews.Portal;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// The signed-in customer reviews their own visits. <b>Contract extension:</b>
/// <c>API Reference.dc.html</c> has no reviews; see <c>docs/contract-extensions.md § 36</c>. The
/// customer id comes from the token only.
/// </summary>
[ApiController]
[Route("portal/reviews")]
[RequireScope(Scopes.Portal)]
public sealed class PortalReviewsController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/reviews/pending</c>: visits of the last 60 days with no review yet, newest
    /// first, and the first name a <c>first_name</c> review would show.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("pending")]
    [ProducesResponseType<PendingReviewsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PendingReviewsDto>> Pending(
        [FromServices] ListPendingReviewsHandler handler,
        CancellationToken cancellationToken)
    {
        var pending = await handler.HandleAsync(cancellationToken);
        Response.Headers.CacheControl = "no-store";

        return Ok(pending);
    }

    /// <summary>
    /// <c>POST /portal/reviews</c>: reviews one of the caller's visits. <c>201</c> with the review,
    /// pending until staff decide. Not theirs is <c>404</c>, already reviewed
    /// <c>409 review_already_submitted</c>, older than 60 days <c>422</c>.
    /// </summary>
    /// <param name="request">The visit, the score, the words and the name choice.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [ProducesResponseType<SubmittedReviewDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubmittedReviewDto>> Submit(
        [FromBody] SubmitPortalReviewRequest request,
        [FromServices] SubmitPortalReviewHandler handler,
        CancellationToken cancellationToken)
    {
        var review = await handler.HandleAsync(
            new SubmitPortalReviewCommand(request.VisitId, request.Rating, request.Text, request.DisplayName),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, review);
    }
}
