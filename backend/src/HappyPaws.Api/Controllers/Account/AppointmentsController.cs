using HappyPaws.Api.Http;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointment;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Account;

/// <summary>
/// <c>GET /appointments/{id}</c> — one of the routes the contract marks "ทั้งสองฝั่ง".
/// <para>
/// No <c>[RequireScope]</c>, deliberately. Staff and customers share this path and a customer
/// holds no <c>appointment:*</c> scope at all, so a scope attribute would lock out half of the
/// intended audience. Authorization happens in <c>AppointmentAccess</c>, which admits staff by
/// scope and branch and customers by care of the pet, and answers <c>404</c> to everyone else.
/// The route is still behind the application's authenticated-by-default fallback policy.
/// </para>
/// </summary>
[ApiController]
[Route("appointments")]
public sealed class AppointmentsController : ControllerBase
{
    /// <summary>Reads one appointment.</summary>
    /// <param name="id">Appointment id — <c>apt_24810</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Get(
        string id,
        [FromServices] GetAppointmentHandler handler,
        CancellationToken cancellationToken)
    {
        var appointment = await handler.HandleAsync(new GetAppointmentQuery(id), cancellationToken);

        // The version a client will echo back in If-Match when it edits this record.
        Response.Headers.ETag = IfMatch.ToETag(appointment.Version);

        return Ok(appointment);
    }
}
