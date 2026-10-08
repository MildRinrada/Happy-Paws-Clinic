using HappyPaws.Api.Authorization;
using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Application.Analytics.GetTodayDashboard;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The front page of the staff console: today's numbers at a glance.</summary>
[ApiController]
[Route("staff/dashboard")]
public sealed class DashboardController : ControllerBase
{
    /// <summary><c>GET /staff/dashboard/today</c> — "การ์ดสรุปหน้าแรกของแต่ละบทบาท".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="branch">One branch; every branch the caller works at when absent.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("today")]
    [RequireScope(Scopes.Dashboard.Read)]
    [ProducesResponseType<TodayDashboardDto>(StatusCodes.Status200OK)]
    public Task<TodayDashboardDto> Today(
        [FromServices] GetTodayDashboardHandler handler,
        [FromQuery] string? branch,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetTodayDashboardQuery(branch), cancellationToken);
}
