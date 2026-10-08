using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Reporting;
using HappyPaws.Application.Reporting.Dtos;
using HappyPaws.Application.Reporting.GetExportStatus;
using HappyPaws.Application.Reporting.StartExport;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>"Reports": asynchronous export jobs, built by the background scheduler.</summary>
[ApiController]
[Route("staff/reports/exports")]
public sealed class ReportExportsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/reports/exports</c> — "งานส่งออกแบบ async · คืน job id". <c>202</c> with the
    /// queued job; poll <c>GET /staff/reports/exports/{id}</c> for the file. Needs the report's own
    /// read scope as well (<c>analytics:read</c>, <c>analytics:hr</c> or <c>stats:read</c>).
    /// </summary>
    /// <param name="request">Which report, which days, which branch.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Report.Export)]
    [ProducesResponseType<ReportExportDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Start(
        [FromBody] StartExportRequest request,
        [FromServices] StartExportHandler handler,
        CancellationToken cancellationToken)
    {
        var export = await handler.HandleAsync(
            new StartExportCommand(request.Report, request.Format, request.From, request.To, request.BranchId),
            cancellationToken);

        return Accepted($"{Request.PathBase}/staff/reports/exports/{export.Id}", export);
    }

    /// <summary>
    /// <c>GET /staff/reports/exports/{id}</c> — "สถานะและลิงก์ดาวน์โหลดอายุ 1 ชั่วโมง". Only the
    /// requester's own jobs; anyone else's is <c>404</c>.
    /// </summary>
    /// <param name="id">The job id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [RequireScope(Scopes.Report.Export)]
    [ProducesResponseType<ReportExportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ReportExportDto> Get(
        string id,
        [FromServices] GetExportStatusHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(id, cancellationToken);
}
