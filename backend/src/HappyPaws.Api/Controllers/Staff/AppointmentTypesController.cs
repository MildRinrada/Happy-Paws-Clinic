using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Api.Http;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointmentRules;
using HappyPaws.Application.Appointments.ReplaceAppointmentTypes;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The clinic's appointment types. Only <c>PUT</c> is in the contract; the <c>GET</c> is
/// <c>docs/contract-extensions.md § 23</c>.
/// </summary>
[ApiController]
[Route("staff/appointment-types")]
public sealed class AppointmentTypesController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/appointment-types</c> — every type with its rules and the set's <c>version</c>
    /// (also the <c>ETag</c>), the same shape the <c>PUT</c> answers with. Not in the API
    /// reference (§ 23).
    /// <para>
    /// On <c>appointment:read</c>, not <c>settings:read</c>: the people who need the list are the
    /// ones who book, and choosing a type is part of booking. Every starter role that books holds
    /// it (receptionist through <c>appointment:*</c>, admin, owner), and the two starter roles that
    /// can <c>PUT</c> the set hold it too. The rules in it say how a visit is booked, nothing about
    /// the clinic's configuration that the calendar does not already show.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Appointment.Read)]
    [ProducesResponseType<AppointmentTypesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentTypesDto>> Get(
        [FromServices] GetAppointmentTypesHandler handler,
        CancellationToken cancellationToken)
    {
        var types = await handler.HandleAsync(cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(types.Version);

        return Ok(types);
    }

    /// <summary>
    /// <c>PUT /staff/appointment-types</c> — "นัดปกติ ฉุกเฉิน ติดตามผล ผ่าตัด · กติกาต่างกัน".
    /// Replaces the whole set; <c>regular</c> must be in it. Accepts <c>If-Match</c>.
    /// </summary>
    /// <param name="request">Every type, with its rules.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut]
    [RequireScope(Scopes.Settings.Write)]
    [ProducesResponseType<AppointmentTypesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AppointmentTypesDto>> Replace(
        [FromBody] ReplaceAppointmentTypesRequest request,
        [FromServices] ReplaceAppointmentTypesHandler handler,
        CancellationToken cancellationToken)
    {
        var types = await handler.HandleAsync(
            new ReplaceAppointmentTypesCommand(
                [.. (request.Types ?? []).Select(type => new AppointmentTypeInput(
                    type.Code,
                    type.Name,
                    type.DurationMinutes,
                    type.OnlineBooking,
                    type.LeadTimeMinutes,
                    type.QueuePriority))],
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(types.Version);

        return Ok(types);
    }
}
