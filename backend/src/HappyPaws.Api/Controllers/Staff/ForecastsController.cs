using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Forecasting.Attendance;
using HappyPaws.Application.Forecasting.Demand;
using HappyPaws.Application.Forecasting.DueBack;
using HappyPaws.Application.Forecasting.Inventory;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The statistical forecasts: four reads worked out from the clinic's own records with plain
/// arithmetic on this server, no model and no outside service. Each sits under the path and the
/// scope of the area it belongs to. None is in the API reference: see
/// <c>docs/contract-extensions.md § 33</c>.
/// </summary>
[ApiController]
[Route("staff")]
public sealed class ForecastsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/inventory/forecast?branch=&amp;cover_days=</c> — per item at one branch: average
    /// daily use over 30 and 90 days, days until out, a suggested order and how sure it is.
    /// </summary>
    /// <param name="branch">One branch; the caller's only branch when absent.</param>
    /// <param name="coverDays">Days of use an order should cover after it arrives, 1 to 180; 30 when absent.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("inventory/forecast")]
    [RequireScope(Scopes.Inventory.Read)]
    [ProducesResponseType<InventoryForecastDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<InventoryForecastDto> InventoryForecast(
        [FromQuery] string? branch,
        [FromQuery(Name = "cover_days")] int? coverDays,
        [FromServices] GetInventoryForecastHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetInventoryForecastQuery(branch, coverDays), cancellationToken);

    /// <summary>
    /// <c>GET /staff/stats/demand-forecast?branch=&amp;from=&amp;to=</c> — per future day and hour:
    /// appointments booked, walk-ins expected, vets on shift, and whether the load per vet is high.
    /// Today or later, at most 14 days.
    /// </summary>
    /// <param name="branch">One branch; the caller's only branch when absent.</param>
    /// <param name="from">First clinic day; today when absent.</param>
    /// <param name="to">Last clinic day, included; a week from <paramref name="from"/> when absent.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("stats/demand-forecast")]
    [RequireScope(Scopes.Stats.Read)]
    [ProducesResponseType<DemandForecastDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<DemandForecastDto> DemandForecast(
        [FromQuery] string? branch,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromServices] GetDemandForecastHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetDemandForecastQuery(branch, from, to), cancellationToken);

    /// <summary>
    /// <c>GET /staff/appointments/attendance-risk?branch=&amp;date=</c> — the day's booked and
    /// confirmed visits, each with a risk of not being attended and the reasons, most at risk first.
    /// </summary>
    /// <param name="branch">One branch; every branch the caller may see when absent.</param>
    /// <param name="date">A clinic day, today or later; today when absent.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("appointments/attendance-risk")]
    [RequireScope(Scopes.Appointment.Read)]
    [ProducesResponseType<AttendanceRiskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<AttendanceRiskDto> AttendanceRisk(
        [FromQuery] string? branch,
        [FromQuery] DateOnly? date,
        [FromServices] GetAttendanceRiskHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAttendanceRiskQuery(branch, date), cancellationToken);

    /// <summary>
    /// <c>GET /staff/customers/due-back?reason=&amp;branch=&amp;limit=&amp;cursor=</c> — a call list of
    /// customers whose pet's vaccine is overdue (<c>vaccine_overdue</c>) or who are later back than
    /// their own usual gap (<c>lapsed</c>), with nothing booked. Most recently due first.
    /// </summary>
    /// <param name="reason"><c>vaccine_overdue</c> or <c>lapsed</c>. Required.</param>
    /// <param name="branch">Customers who have visited this branch; every branch the caller may see when absent.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("customers/due-back")]
    [RequireScope(Scopes.Crm.Read)]
    [ProducesResponseType<CursorPageResponse<DueBackDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CursorPageResponse<DueBackDto>>> DueBack(
        [FromQuery] string? reason,
        [FromQuery] string? branch,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListDueBackHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListDueBackQuery(reason, branch, limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        return Ok(CursorPageResponse<DueBackDto>.From(page));
    }
}
