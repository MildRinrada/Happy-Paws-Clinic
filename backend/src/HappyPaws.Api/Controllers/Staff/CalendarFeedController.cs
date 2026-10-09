using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.GetCalendarFeed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The appointment calendar feed (iCalendar).</summary>
[ApiController]
[Route("staff/integrations/calendar/ics")]
public sealed class CalendarFeedController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/integrations/calendar/ics</c> — appointments as an iCalendar feed.
    /// <list type="bullet">
    ///   <item><description><c>?token=hpcal_…</c> with no login: the link a calendar app subscribes to (calendar apps cannot send a bearer token).</description></item>
    ///   <item><description>Signed in: the same feed as a file; or, with <c>Accept: application/json</c>, the caller's subscription link.</description></item>
    /// </list>
    /// <c>?view=mine</c> (default) is the caller's own appointments; <c>?view=branch</c> is every
    /// appointment in their branches and needs <c>appointment:read</c>. Service, pet name, branch
    /// and time only; nothing clinical. A bad token is <c>401</c>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="token">The subscription token, for calendar apps.</param>
    /// <param name="view"><c>mine</c> or <c>branch</c>.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [AllowAnonymous]
    [Produces("text/calendar", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<CalendarLinkDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromServices] CalendarFeedHandler handler,
        [FromQuery] string? token,
        [FromQuery] string? view,
        CancellationToken cancellationToken)
    {
        var chosen = ParseView(view);

        // No cache may keep a feed, or the link that unlocks it.
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Robots-Tag"] = "noindex";

        if (!string.IsNullOrEmpty(token))
        {
            var feed = await handler.RenderForTokenAsync(token, chosen, cancellationToken);
            return Content(feed.Content, IcsCalendar.ContentType);
        }

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            throw new UnauthenticatedException();
        }

        if (WantsJson())
        {
            return Ok(await handler.GetLinkAsync(cancellationToken));
        }

        var file = await handler.RenderForCallerAsync(chosen, cancellationToken);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{file.FileName}\"";

        return Content(file.Content, IcsCalendar.ContentType);
    }

    private bool WantsJson() =>
        Request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

    private static CalendarView ParseView(string? view) => view?.Trim().ToLowerInvariant() switch
    {
        null or "" or "mine" => CalendarView.Mine,
        "branch" => CalendarView.Branch,
        _ => throw new ValidationFailedException(new Dictionary<string, string[]>
        {
            ["view"] = ["view ต้องเป็น mine หรือ branch"],
        }),
    };
}
