using HappyPaws.Api.Authorization;
using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Application.Analytics.GetRetentionAnalytics;
using HappyPaws.Application.Analytics.GetRevenueAnalytics;
using HappyPaws.Application.Analytics.GetServiceAnalytics;
using HappyPaws.Application.Analytics.GetStaffPerformance;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// "Business dashboard": money, services, customers and people. Three reads are
/// <c>analytics:read</c>; staff performance ranks people and is <c>analytics:hr</c>. Days are
/// clinic days, both ends included, and a span is at most 366 days.
/// </summary>
[ApiController]
[Route("staff/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    /// <summary><c>GET /staff/analytics/revenue</c> — "รายวัน/เดือน แยกสาขาและช่องทางชำระ".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">First clinic day (<c>YYYY-MM-DD</c>).</param>
    /// <param name="to">Last clinic day, included.</param>
    /// <param name="branch">One branch; every branch the caller works at when absent.</param>
    /// <param name="granularity"><c>day</c>, <c>week</c> or <c>month</c>.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("revenue")]
    [RequireScope(Scopes.Analytics.Read)]
    [ProducesResponseType<RevenueAnalyticsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<RevenueAnalyticsDto> Revenue(
        [FromServices] GetRevenueAnalyticsHandler handler,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? branch,
        [FromQuery] string? granularity,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetRevenueAnalyticsQuery(from, to, branch, granularity), cancellationToken);

    /// <summary><c>GET /staff/analytics/services</c> — "บริการทำเงิน · กำไรต่อบริการหลังหักยา".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">First clinic day.</param>
    /// <param name="to">Last clinic day, included.</param>
    /// <param name="branch">One branch; every branch the caller works at when absent.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("services")]
    [RequireScope(Scopes.Analytics.Read)]
    [ProducesResponseType<ServiceAnalyticsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ServiceAnalyticsDto> Services(
        [FromServices] GetServiceAnalyticsHandler handler,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? branch,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetServiceAnalyticsQuery(from, to, branch), cancellationToken);

    /// <summary><c>GET /staff/analytics/retention</c> — "ลูกค้าใหม่/กลับมา · cohort รายเดือน".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">Any day in the first month; six months back when absent.</param>
    /// <param name="to">Last clinic day counted.</param>
    /// <param name="branch">One branch; every branch the caller works at when absent.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("retention")]
    [RequireScope(Scopes.Analytics.Read)]
    [ProducesResponseType<RetentionAnalyticsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<RetentionAnalyticsDto> Retention(
        [FromServices] GetRetentionAnalyticsHandler handler,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? branch,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetRetentionAnalyticsQuery(from, to, branch), cancellationToken);

    /// <summary><c>GET /staff/analytics/staff-performance</c> — "เคส รายได้ และคะแนนรีวิวต่อคน".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">First clinic day.</param>
    /// <param name="to">Last clinic day, included.</param>
    /// <param name="branch">One branch; every branch the caller works at when absent.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("staff-performance")]
    [RequireScope(Scopes.Analytics.Hr)]
    [ProducesResponseType<StaffPerformanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<StaffPerformanceDto> StaffPerformance(
        [FromServices] GetStaffPerformanceHandler handler,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? branch,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetStaffPerformanceQuery(from, to, branch), cancellationToken);
}
