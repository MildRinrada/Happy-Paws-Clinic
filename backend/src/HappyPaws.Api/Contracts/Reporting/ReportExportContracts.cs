namespace HappyPaws.Api.Contracts.Reporting;

/// <summary>
/// <c>POST /staff/reports/exports</c>:
/// <c>{ "report": "revenue", "format": "csv", "from": "2026-09-01", "to": "2026-09-30", "branch_id": "brn_..." }</c>.
/// <c>report</c> is one of <c>revenue</c>, <c>services</c>, <c>retention</c>,
/// <c>staff_performance</c>, <c>appointments</c>, <c>queue</c>, <c>workload</c>. <c>format</c> is
/// <c>csv</c> (the default), <c>xlsx</c> or <c>pdf</c>; the API reference names no format, and the
/// last two are an extension the clinic asked for (<c>docs/contract-extensions.md § 21</c>).
/// Everything else is optional, with the same defaults as the report's own screen.
/// </summary>
public sealed record StartExportRequest(
    string? Report = null,
    string? Format = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? BranchId = null);
