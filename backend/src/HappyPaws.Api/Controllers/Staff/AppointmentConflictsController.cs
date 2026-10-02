using HappyPaws.Api.Authorization;
using HappyPaws.Application.Appointments.DetectConflicts;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Checking a time before it is booked, or a day after the roster changed.</summary>
[ApiController]
[Route("staff/appointments/conflicts")]
public sealed class AppointmentConflictsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/appointments/conflicts</c> — "ตรวจนัดชน หมอลา ห้องไม่ว่าง ก่อนยืนยัน".
    /// <para>
    /// With <c>starts_at</c> (and <c>ends_at</c> or <c>service_id</c>) it checks a proposed visit;
    /// with <c>date</c> it checks the visits already on that day. Rooms are not checked: an
    /// appointment does not hold one.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="startsAt">Start of the proposed visit.</param>
    /// <param name="endsAt">End of the proposed visit.</param>
    /// <param name="serviceId">The service, for its length when <paramref name="endsAt"/> is not given.</param>
    /// <param name="vetId">The vet to check.</param>
    /// <param name="petId">The patient to check.</param>
    /// <param name="branch">The branch.</param>
    /// <param name="exclude">An appointment being moved, so it does not clash with itself.</param>
    /// <param name="date">A clinic day whose existing visits to check.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Appointment.Read)]
    [ProducesResponseType<ConflictReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ConflictReportDto> Detect(
        [FromServices] DetectConflictsHandler handler,
        [FromQuery(Name = "starts_at")] DateTimeOffset? startsAt,
        [FromQuery(Name = "ends_at")] DateTimeOffset? endsAt,
        [FromQuery(Name = "service_id")] string? serviceId,
        [FromQuery(Name = "vet_id")] string? vetId,
        [FromQuery(Name = "pet_id")] string? petId,
        [FromQuery] string? branch,
        [FromQuery] string? exclude,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new DetectConflictsQuery(startsAt, endsAt, serviceId, vetId, petId, branch, exclude, date),
            cancellationToken);
}
