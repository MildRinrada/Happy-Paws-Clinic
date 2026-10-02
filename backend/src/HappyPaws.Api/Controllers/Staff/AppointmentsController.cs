using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Appointments.CreateStaffAppointment;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.ListAppointments;
using HappyPaws.Application.Appointments.MarkNoShow;
using HappyPaws.Application.Appointments.UpdateAppointment;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The clinic's own appointment surface — booking on a customer's behalf, the day's board, and
/// edits.
/// </summary>
[ApiController]
[Route("staff/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/appointments</c> — "จองแทนลูกค้าและรับ walk-in · ข้าม lead time ได้".
    /// </summary>
    /// <param name="request">Pet, service, start time and whether this is a walk-in.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [Idempotent]
    [RequireScope(Scopes.Appointment.Write)]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AppointmentDto>> Create(
        [FromBody] CreateStaffAppointmentRequest request,
        [FromServices] CreateStaffAppointmentHandler handler,
        CancellationToken cancellationToken)
    {
        var appointment = await handler.HandleAsync(
            new CreateStaffAppointmentCommand(
                request.PetId,
                request.ServiceId,
                request.StartsAt,
                request.StaffId,
                request.BranchId,
                request.Reason,
                request.WalkIn,
                request.Type),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(appointment.Version);

        return Created($"/appointments/{appointment.Id}", appointment);
    }

    /// <summary>
    /// <c>GET /staff/appointments?date=&amp;status=</c> — "ตารางวันนี้ของทั้งคลินิกหรือรายหมอ".
    /// </summary>
    /// <param name="date">Clinic-local day to show.</param>
    /// <param name="status">One of the contract's appointment states, in snake_case.</param>
    /// <param name="vetId">Narrows the board to one vet.</param>
    /// <param name="branch">Narrows to one branch, subject to the caller's own branch assignment.</param>
    /// <param name="q">Free-text search over the patient's name or hospital number.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from a previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Appointment.Read)]
    [ProducesResponseType<CursorPageResponse<AppointmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPageResponse<AppointmentDto>>> List(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        [FromQuery(Name = "vet_id")] string? vetId,
        [FromQuery] string? branch,
        [FromQuery] string? q,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListAppointmentsHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListAppointmentsQuery(
                date,
                status,
                vetId,
                branch,
                q,
                limit == 0 ? CursorPaging.DefaultLimit : limit,
                cursor),
            cancellationToken);

        return Ok(CursorPageResponse<AppointmentDto>.From(page));
    }

    /// <summary>
    /// <c>PATCH /staff/appointments/{id}</c> — "เปลี่ยนหมอ บริการ หรืออาการที่แจ้ง".
    /// Requires <c>If-Match</c>; see the handler for why it is mandatory here.
    /// </summary>
    /// <param name="id">Appointment id.</param>
    /// <param name="request">The fields to change; omitted fields are left alone.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [RequireScope(Scopes.Appointment.Write)]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Update(
        string id,
        [FromBody] UpdateAppointmentRequest request,
        [FromServices] UpdateAppointmentHandler handler,
        CancellationToken cancellationToken)
    {
        var appointment = await handler.HandleAsync(
            new UpdateAppointmentCommand(
                id,
                request.ServiceId,
                request.StaffId,
                request.Reason,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(appointment.Version);

        return Ok(appointment);
    }

    /// <summary>
    /// <c>POST /staff/appointments/{id}/no-show</c> — "บันทึกไม่มาตามนัด · นับสถิติรายลูกค้า".
    /// <para>
    /// Only after the start time, and only from <c>booked</c> or <c>confirmed</c>. The answer
    /// carries the customer's record under the cancellation policy, including any fee due, which
    /// is reported rather than charged.
    /// </para>
    /// </summary>
    /// <param name="id">Appointment id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/no-show")]
    [RequireScope(Scopes.Appointment.Write)]
    [ProducesResponseType<NoShowResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<NoShowResultDto> NoShow(
        string id,
        [FromServices] MarkNoShowHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new MarkNoShowCommand(id), cancellationToken);
}
