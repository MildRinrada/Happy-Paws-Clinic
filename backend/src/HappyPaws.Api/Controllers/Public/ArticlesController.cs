using HappyPaws.Api.Contracts;
using HappyPaws.Application.Articles.Dtos;
using HappyPaws.Application.Articles.GetArticle;
using HappyPaws.Application.Articles.ListPublishedArticles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// The clinic's health articles. <b>Contract extension:</b> <c>API Reference.dc.html</c> has no
/// articles at all; these two reads were authorised so the public site can show the vets'
/// articles. See <c>docs/contract-extensions.md § 9</c>.
/// <para>
/// Public and read-only. Articles arrive from the operator's <c>seed-articles</c> command; there is
/// no endpoint that writes one.
/// </para>
/// </summary>
[ApiController]
[Route("articles")]
[AllowAnonymous]
public sealed class ArticlesController : ControllerBase
{
    /// <summary><c>GET /articles</c> — published articles, newest first. Contract extension.</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="limit">How many; 1 to 50, 10 when absent.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<ListResponse<ArticleSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResponse<ArticleSummaryDto>>> List(
        [FromServices] ListPublishedArticlesHandler handler,
        [FromQuery] int? limit,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(
            await handler.HandleAsync(new ListPublishedArticlesQuery(limit), cancellationToken)));

    /// <summary><c>GET /articles/{slug}</c> — one published article. Contract extension.</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="slug">The article's address.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{slug}")]
    [ProducesResponseType<ArticleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArticleDto>> Get(
        [FromServices] GetArticleHandler handler,
        string slug,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new GetArticleQuery(slug), cancellationToken));
}
