using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAvailability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// <c>GET /booking/availability</c> — "slot ว่างตาม service + vet + branch + วันที่".
/// <para>
/// Public, as the contract marks it: a clinic's booking page has to work before anyone signs in.
/// <c>[AllowAnonymous]</c> is required because the application's fallback policy demands an
/// authenticated caller on every other route.
/// </para>
/// </summary>
[ApiController]
[Route("booking/availability")]
[AllowAnonymous]
public sealed class BookingAvailabilityController : ControllerBase
{
    /// <summary>Openings for a service on a day, optionally narrowed to one vet or branch.</summary>
    /// <param name="service">Service id — <c>svc_checkup</c>.</param>
    /// <param name="date">Clinic-local day, <c>2026-08-21</c>.</param>
    /// <param name="vet">Optional staff id to restrict the search to.</param>
    /// <param name="branch">Optional branch id; may be omitted at a single-site clinic.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<AvailabilityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityDto>> Get(
        [FromQuery] string service,
        [FromQuery] DateOnly date,
        [FromQuery] string? vet,
        [FromQuery] string? branch,
        [FromServices] GetAvailabilityHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new GetAvailabilityQuery(service, date, vet, branch), cancellationToken));
}
