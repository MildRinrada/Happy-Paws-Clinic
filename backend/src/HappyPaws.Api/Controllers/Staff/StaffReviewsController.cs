using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Reviews;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Reviews;
using HappyPaws.Application.Reviews.Staff;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Moderating customer reviews before they reach the public site. <b>Contract extension:</b> see
/// <c>docs/contract-extensions.md § 36</c>. Every route is on <c>content:manage</c>, the scope of
/// the public site's articles: what the site says about the clinic is the same desk's job.
/// <para>
/// Staff publish, reject for a listed reason, or answer. They cannot edit a customer's words, and
/// every decision is audited with their staff id.
/// </para>
/// </summary>
[ApiController]
[Route("staff/reviews")]
public sealed class StaffReviewsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/reviews?status=&amp;limit=&amp;cursor=</c>: reviews of a status (<c>pending</c>,
    /// <c>published</c>, <c>rejected</c>) or of every status, newest written first, with who wrote
    /// each and about which pet.
    /// </summary>
    /// <param name="status">The tab.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<CursorPageResponse<StaffReviewDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CursorPageResponse<StaffReviewDto>>> List(
        [FromQuery] string? status,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListStaffReviewsHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListStaffReviewsQuery(status, limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Ok(CursorPageResponse<StaffReviewDto>.From(page));
    }

    /// <summary>
    /// <c>POST /staff/reviews/{id}/publish</c>: puts a pending review on the public site. Not
    /// pending is <c>409 state_conflict</c>. Audited as <c>review.published</c>.
    /// </summary>
    /// <param name="id">The review.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/publish")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<StaffReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffReviewDto>> Publish(
        string id,
        [FromServices] PublishReviewHandler handler,
        CancellationToken cancellationToken) =>
        Answer(await handler.HandleAsync(new PublishReviewCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken));

    /// <summary>
    /// <c>POST /staff/reviews/{id}/reject</c>: keeps a review off the site, or takes a published one
    /// down, for a listed reason. No reason is <c>422</c>; already rejected <c>409 state_conflict</c>.
    /// Audited as <c>review.rejected</c>.
    /// </summary>
    /// <param name="id">The review.</param>
    /// <param name="request">The reason.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/reject")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<StaffReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StaffReviewDto>> Reject(
        string id,
        [FromBody] RejectReviewRequest request,
        [FromServices] RejectReviewHandler handler,
        CancellationToken cancellationToken) =>
        Answer(await handler.HandleAsync(new RejectReviewCommand(id, request.Reason, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken));

    /// <summary>
    /// <c>POST /staff/reviews/{id}/reply</c>: writes or rewrites the clinic's one public answer.
    /// A rejected review is <c>409 state_conflict</c>. Audited as <c>review.replied</c>.
    /// </summary>
    /// <param name="id">The review.</param>
    /// <param name="request">The answer.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/reply")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<StaffReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StaffReviewDto>> Reply(
        string id,
        [FromBody] ReplyToReviewRequest request,
        [FromServices] ReplyToReviewHandler handler,
        CancellationToken cancellationToken) =>
        Answer(await handler.HandleAsync(new ReplyToReviewCommand(id, request.Text, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken));

    private ActionResult<StaffReviewDto> Answer(StaffReviewDto review)
    {
        Response.Headers.ETag = IfMatch.ToETag(review.Version);
        return Ok(review);
    }
}
