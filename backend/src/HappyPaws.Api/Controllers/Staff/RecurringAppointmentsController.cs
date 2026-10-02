using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Appointments.CreateRecurringSeries;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>A course of visits booked in one request.</summary>
[ApiController]
[Route("staff/appointments/recurring")]
public sealed class RecurringAppointmentsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/appointments/recurring</c> — "คอร์สรักษาต่อเนื่อง เช่น เคมีบำบัดทุก 3 สัปดาห์".
    /// <para>
    /// Every visit or none: when some dates cannot be booked the answer is
    /// <c>422 slot_unavailable</c> with one entry per date in <c>details.conflicts</c>. Requires
    /// <c>Idempotency-Key</c>, because it creates appointments.
    /// </para>
    /// </summary>
    /// <param name="request">The pet, the service, the first visit and the repeat rule.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [Idempotent]
    [RequireScope(Scopes.Appointment.Write)]
    [ProducesResponseType<RecurringSeriesDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RecurringSeriesDto>> Create(
        [FromBody] CreateRecurringSeriesRequest request,
        [FromServices] CreateRecurringSeriesHandler handler,
        CancellationToken cancellationToken)
    {
        var series = await handler.HandleAsync(
            new CreateRecurringSeriesCommand(
                request.PetId,
                request.ServiceId,
                request.FirstStartsAt,
                request.Rrule,
                request.StaffId,
                request.BranchId,
                request.Reason,
                request.Type),
            cancellationToken);

        return Created($"/staff/appointments?series_id={series.SeriesId}", series);
    }
}
