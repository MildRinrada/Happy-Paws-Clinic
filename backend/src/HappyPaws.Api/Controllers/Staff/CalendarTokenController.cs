using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.RotateCalendarFeed;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>A new calendar link for the signed-in staff member.</summary>
[ApiController]
[Route("staff/integrations/calendar/token")]
public sealed class CalendarTokenController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/integrations/calendar/token</c> — replaces the caller's calendar link; the
    /// old link is <c>401</c> from now on. Answers the new link, as <c>GET …/calendar/ics</c> with
    /// <c>Accept: application/json</c> does. Any signed-in staff member, for their own link only;
    /// no scope, and never an API key (the fallback policy refuses one). Not in the API reference;
    /// see <c>docs/contract-extensions.md § 22</c>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [ProducesResponseType<CalendarLinkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CalendarLinkDto>> Rotate(
        [FromServices] RotateCalendarFeedHandler handler,
        CancellationToken cancellationToken)
    {
        var link = await handler.HandleAsync(cancellationToken);

        Response.Headers.CacheControl = "private, no-store";

        return Ok(link);
    }
}
