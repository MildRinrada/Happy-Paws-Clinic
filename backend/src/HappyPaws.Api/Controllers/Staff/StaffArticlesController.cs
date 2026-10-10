using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Articles;
using HappyPaws.Api.Http;
using HappyPaws.Application.Articles.Manage;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Writing and publishing the health articles on the public site. <b>Contract extension:</b>
/// <c>API Reference.dc.html</c> has no articles at all; these routes and their scope,
/// <c>content:manage</c>, were approved together. See <c>docs/contract-extensions.md § 26</c>.
/// <para>
/// Every route is on <c>content:manage</c>, the reads included: a draft is the clinic's unfinished
/// public statement, and only the people who write them need to see one.
/// </para>
/// </summary>
[ApiController]
[Route("staff/articles")]
public sealed class StaffArticlesController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/articles?status=&amp;q=&amp;limit=&amp;cursor=</c> — every article that is not deleted,
    /// drafts included, newest written first.
    /// </summary>
    /// <param name="status"><c>draft</c>, <c>scheduled</c> or <c>published</c>.</param>
    /// <param name="q">Part of the title or the slug.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<CursorPageResponse<StaffArticleSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CursorPageResponse<StaffArticleSummaryDto>>> List(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListStaffArticlesHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListStaffArticlesQuery(status, q, limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        return Ok(CursorPageResponse<StaffArticleSummaryDto>.From(page));
    }

    /// <summary><c>GET /staff/articles/{id}</c> — one article for the editor, with an <c>ETag</c>.</summary>
    /// <param name="id">The article.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<StaffArticleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffArticleDto>> Get(
        string id,
        [FromServices] GetStaffArticleHandler handler,
        CancellationToken cancellationToken)
    {
        var article = await handler.HandleAsync(id, cancellationToken);
        Response.Headers.ETag = IfMatch.ToETag(article.Version);

        return Ok(article);
    }

    /// <summary>
    /// <c>POST /staff/articles</c> — writes a draft. Nothing is public until it is published. A slug
    /// another article holds, deleted ones included, is <c>409 article_slug_taken</c>. Audited as
    /// <c>article.created</c>.
    /// </summary>
    /// <param name="request">The draft.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<StaffArticleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StaffArticleDto>> Create(
        [FromBody] CreateArticleRequest request,
        [FromServices] CreateArticleHandler handler,
        CancellationToken cancellationToken)
    {
        var article = await handler.HandleAsync(
            new CreateArticleCommand(
                request.Title,
                request.Summary,
                request.Body,
                request.AuthorName,
                request.Slug,
                request.ReadingMinutes),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(article.Version);

        return Created($"/staff/articles/{article.Id}", article);
    }

    /// <summary>
    /// <c>PATCH /staff/articles/{id}</c> — changes what was sent. <c>If-Match</c> is required. The
    /// slug is fixed once the article has ever been published (<c>409 state_conflict</c>). Audited as
    /// <c>article.updated</c>.
    /// </summary>
    /// <param name="id">The article.</param>
    /// <param name="request">What to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<StaffArticleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StaffArticleDto>> Update(
        string id,
        [FromBody] UpdateArticleRequest request,
        [FromServices] UpdateArticleHandler handler,
        CancellationToken cancellationToken)
    {
        var article = await handler.HandleAsync(
            new UpdateArticleCommand(
                id,
                request.Slug,
                request.Title,
                request.Summary,
                request.Body,
                request.AuthorName,
                request.ReadingMinutes,
                request.RemoveCover,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(article.Version);

        return Ok(article);
    }

    /// <summary>
    /// <c>POST /staff/articles/{id}/publish</c> — on the public site now. Already public is
    /// <c>409 state_conflict</c>. <c>If-Match</c> honoured. Audited as <c>article.published</c>.
    /// </summary>
    /// <param name="id">The article.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/publish")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<StaffArticleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffArticleDto>> Publish(
        string id,
        [FromServices] PublishArticleHandler handler,
        CancellationToken cancellationToken)
    {
        var article = await handler.HandleAsync(
            new PublishArticleCommand(id, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(article.Version);

        return Ok(article);
    }

    /// <summary>
    /// <c>POST /staff/articles/{id}/unpublish</c> — off the public site, a draft again. A draft is
    /// <c>409 state_conflict</c>. <c>If-Match</c> honoured. Audited as <c>article.unpublished</c>.
    /// </summary>
    /// <param name="id">The article.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/unpublish")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<StaffArticleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffArticleDto>> Unpublish(
        string id,
        [FromServices] UnpublishArticleHandler handler,
        CancellationToken cancellationToken)
    {
        var article = await handler.HandleAsync(
            new UnpublishArticleCommand(id, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(article.Version);

        return Ok(article);
    }

    /// <summary>
    /// <c>DELETE /staff/articles/{id}</c> — deletes in any state; a published article leaves the site
    /// in the same step. The slug stays taken. <c>204</c>; a second delete is <c>404</c>.
    /// <c>If-Match</c> honoured. Audited as <c>article.deleted</c>.
    /// </summary>
    /// <param name="id">The article.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{id}")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        string id,
        [FromServices] DeleteArticleHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteArticleCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// <c>POST /staff/articles/{id}/cover</c> — a signed URL to <c>PUT</c> a new cover to (JPEG, PNG
    /// or WebP, at most 5 MB). The current cover stays until the file arrives. <c>If-Match</c>
    /// honoured. Audited as <c>article.updated</c>.
    /// </summary>
    /// <param name="id">The article.</param>
    /// <param name="request">The file's type and size.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/cover")]
    [RequireScope(Scopes.Content.Manage)]
    [ProducesResponseType<ArticleCoverUploadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ArticleCoverUploadDto>> RequestCover(
        string id,
        [FromBody] ArticleCoverRequest request,
        [FromServices] RequestArticleCoverHandler handler,
        CancellationToken cancellationToken)
    {
        var started = await handler.HandleAsync(
            new RequestArticleCoverCommand(id, request.ContentType, request.SizeBytes, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Ok(started);
    }
}
