using HappyPaws.Api.Authorization;
using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Application.Analytics.GetAppointmentStats;
using HappyPaws.Application.Analytics.GetQueueStats;
using HappyPaws.Application.Analytics.GetWorkloadStats;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// "Operational statistics": how the clinic ran over a span of days. All three read
/// <c>?from=&amp;to=</c> as clinic days with both ends included, default to the last 30 days, and
/// refuse a span longer than 366 days with <c>422</c>.
/// </summary>
[ApiController]
[Route("staff/stats")]
[RequireScope(Scopes.Stats.Read)]
public sealed class StatsController : ControllerBase
{
    /// <summary><c>GET /staff/stats/appointments</c> — "ปริมาณนัด อัตรายกเลิก no-show รายช่วง".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">First clinic day (<c>YYYY-MM-DD</c>).</param>
    /// <param name="to">Last clinic day, included.</param>
    /// <param name="branch">One branch; every branch the caller works at when absent.</param>
    /// <param name="granularity"><c>day</c>, <c>week</c> or <c>month</c>.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("appointments")]
    [ProducesResponseType<AppointmentStatsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<AppointmentStatsDto> Appointments(
        [FromServices] GetAppointmentStatsHandler handler,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? branch,
        [FromQuery] string? granularity,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAppointmentStatsQuery(from, to, branch, granularity), cancellationToken);

    /// <summary><c>GET /staff/stats/queue</c> — "เวลารอเฉลี่ย ชั่วโมงพีค คิวคงค้าง".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">First service day.</param>
    /// <param name="to">Last service day, included.</param>
    /// <param name="branch">One branch; every branch the caller works at when absent.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("queue")]
    [ProducesResponseType<QueueStatsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<QueueStatsDto> Queue(
        [FromServices] GetQueueStatsHandler handler,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? branch,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetQueueStatsQuery(from, to, branch), cancellationToken);

    /// <summary><c>GET /staff/stats/workload</c> — "เคสต่อหมอ เวลาเฉลี่ยต่อเคส งานค้าง".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">First clinic day.</param>
    /// <param name="to">Last clinic day, included.</param>
    /// <param name="branch">One branch; every branch the caller works at when absent.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("workload")]
    [ProducesResponseType<WorkloadStatsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<WorkloadStatsDto> Workload(
        [FromServices] GetWorkloadStatsHandler handler,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? branch,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetWorkloadStatsQuery(from, to, branch), cancellationToken);
}
