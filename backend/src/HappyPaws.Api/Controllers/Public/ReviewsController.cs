using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Reviews;
using HappyPaws.Application.Reviews.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// The clinic's published reviews. <b>Contract extension:</b> see <c>docs/contract-extensions.md § 36</c>.
/// Public and read-only, published reviews only, and never a surname, phone, email or pet's name.
/// </summary>
[ApiController]
[Route("reviews")]
[AllowAnonymous]
public sealed class ReviewsController : ControllerBase
{
    /// <summary>
    /// <c>GET /reviews?limit=&amp;cursor=</c>: published reviews, newest written first, with
    /// <c>summary</c> (<c>average</c>, <c>count</c>, <c>by_rating</c>) over every published review.
    /// May be cached for a minute.
    /// </summary>
    /// <param name="limit">Page size, 1 to 100; 25 when absent.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> List(
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListPublicReviewsHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListPublicReviewsQuery(limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        Response.Headers.CacheControl = "public, max-age=60";

        // The shared list envelope, with the summary beside the page.
        return Ok(new
        {
            data = page.Data,
            page = new { next_cursor = page.NextCursor },
            summary = page.Summary,
        });
    }
}
