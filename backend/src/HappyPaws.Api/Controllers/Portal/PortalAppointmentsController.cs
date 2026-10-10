using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Appointments.BookAppointment;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.ListMyAppointments;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// The customer's own booking surface — <c>/portal/appointments</c>.
/// </summary>
[ApiController]
[Route("portal/appointments")]
[RequireScope(Scopes.Portal)]
public sealed class PortalAppointmentsController : ControllerBase
{
    /// <summary>
    /// <c>POST /portal/appointments</c> — "ลูกค้ายืนยันการจอง · ต้องมี Idempotency-Key".
    /// <para>
    /// The header requirement is the contract's, and it is enforced rather than encouraged: a
    /// double-tapped Confirm on a phone is the exact scenario it exists for.
    /// </para>
    /// </summary>
    /// <param name="request">Pet, service, start time and the hold being redeemed.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [Idempotent]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AppointmentDto>> Book(
        [FromBody] BookAppointmentRequest request,
        [FromServices] BookAppointmentHandler handler,
        CancellationToken cancellationToken)
    {
        var appointment = await handler.HandleAsync(
            new BookAppointmentCommand(
                request.PetId,
                request.ServiceId,
                request.StartsAt,
                request.StaffId,
                request.BranchId,
                request.HoldId,
                request.Reason,
                request.Type,
                request.NewPet is null ? null : new NewPetInput(request.NewPet.Name, request.NewPet.Species)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(appointment.Version);

        return Created($"/appointments/{appointment.Id}", appointment);
    }

    /// <summary><c>GET /portal/appointments</c> — "นัดที่จะถึงและประวัติย้อนหลัง".</summary>
    /// <param name="upcoming">True (the default) for future visits; false for the history.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from a previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<CursorPageResponse<AppointmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPageResponse<AppointmentDto>>> List(
        [FromQuery] bool? upcoming,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListMyAppointmentsHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListMyAppointmentsQuery(
                // The portal opens on "what's next", so an absent flag means upcoming.
                upcoming ?? true,
                limit == 0 ? CursorPaging.DefaultLimit : limit,
                cursor),
            cancellationToken);

        return Ok(CursorPageResponse<AppointmentDto>.From(page));
    }
}
