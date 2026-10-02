using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Appointments.CancelAppointment;
using HappyPaws.Application.Appointments.CheckInAppointment;
using HappyPaws.Application.Appointments.ConfirmAppointment;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.RescheduleAppointment;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Account;

/// <summary>
/// The four state-changing actions on <c>/appointments/{id}</c>, all marked "ทั้งสองฝั่ง" in the
/// contract: reschedule, cancel, confirm and check-in.
/// <para>
/// Like <see cref="AppointmentsController"/>, these carry no scope attribute — the audience fork
/// is a resource-level decision made in <c>AppointmentAccess</c>, because a customer legitimately
/// calling these holds no staff scope. Check-in is the clearest case: the contract lists
/// <c>appointment:checkin</c> and also says customers check themselves in.
/// </para>
/// </summary>
[ApiController]
[Route("appointments/{id}")]
public sealed class AppointmentActionsController : ControllerBase
{
    /// <summary><c>POST /appointments/{id}/reschedule</c> — "ลูกค้าเลื่อนได้ถ้ายังไม่ถึง cutoff".</summary>
    /// <param name="id">Appointment id.</param>
    /// <param name="request">The new start time, and optionally a different vet.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("reschedule")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AppointmentDto>> Reschedule(
        string id,
        [FromBody] RescheduleAppointmentRequest request,
        [FromServices] RescheduleAppointmentHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new RescheduleAppointmentCommand(
                id,
                request.StartsAt,
                request.StaffId,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken));

    /// <summary><c>POST /appointments/{id}/cancel</c> — "บันทึกเหตุผลและผู้ยกเลิก · ปล่อย slot คืน".</summary>
    /// <param name="id">Appointment id.</param>
    /// <param name="request">The cancellation reason, recorded against the appointment.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("cancel")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Cancel(
        string id,
        [FromBody] CancelAppointmentRequest? request,
        [FromServices] CancelAppointmentHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new CancelAppointmentCommand(id, request?.Reason), cancellationToken));

    /// <summary><c>POST /appointments/{id}/confirm</c> — "ยืนยันจากลิงก์เตือนหรือปุ่มใน LINE".</summary>
    /// <param name="id">Appointment id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("confirm")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Confirm(
        string id,
        [FromServices] ConfirmAppointmentHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(new ConfirmAppointmentCommand(id), cancellationToken));

    /// <summary>
    /// <c>POST /appointments/{id}/check-in</c> — "เช็กอินที่เคาน์เตอร์หรือลูกค้าเช็กอินเอง · สร้างคิวทันที".
    /// Idempotent, because it creates a queue ticket and the contract requires the header on every
    /// POST that creates one.
    /// </summary>
    /// <param name="id">Appointment id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("check-in")]
    [Idempotent]
    [ProducesResponseType<CheckInResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckInResultDto>> CheckIn(
        string id,
        [FromServices] CheckInAppointmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CheckInAppointmentCommand(id), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(result.Appointment.Version);

        return Ok(result);
    }

    private ActionResult<AppointmentDto> WithETag(AppointmentDto appointment)
    {
        Response.Headers.ETag = IfMatch.ToETag(appointment.Version);
        return Ok(appointment);
    }
}
