using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Api.Http;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.ReplaceAppointmentTypes;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The clinic's appointment types. Only <c>PUT</c> is in the contract; it answers with the whole set.</summary>
[ApiController]
[Route("staff/appointment-types")]
public sealed class AppointmentTypesController : ControllerBase
{
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
