using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointmentRules;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// The appointment types a customer may book — <c>/portal/appointment-types</c>. Not in the API
/// reference: see <c>docs/contract-extensions.md § 23</c>.
/// </summary>
[ApiController]
[Route("portal/appointment-types")]
[RequireScope(Scopes.Portal)]
public sealed class PortalAppointmentTypesController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/appointment-types</c> — only the types with <c>online_booking</c> on, as
    /// <c>{ "data": [ { "code", "name", "duration_minutes" } ] }</c>. The booking form offers these
    /// and nothing else, so it never offers a type <c>POST /portal/appointments</c> refuses. On the
    /// portal scope, like the booking it serves; the counter's rules are not in it.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<ListResponse<OnlineAppointmentTypeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResponse<OnlineAppointmentTypeDto>>> List(
        [FromServices] ListOnlineAppointmentTypesHandler handler,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(await handler.HandleAsync(cancellationToken)));
}
