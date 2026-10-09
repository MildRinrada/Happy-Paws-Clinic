using HappyPaws.Application.Analytics.GetAppointmentStats;
using HappyPaws.Application.Analytics.GetQueueStats;
using HappyPaws.Application.Analytics.GetRetentionAnalytics;
using HappyPaws.Application.Analytics.GetRevenueAnalytics;
using HappyPaws.Application.Analytics.GetServiceAnalytics;
using HappyPaws.Application.Analytics.GetStaffPerformance;
using HappyPaws.Application.Analytics.GetWorkloadStats;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.Application.Reporting;

/// <summary>One exportable report: the scope its screen needs, and how long a span it covers by default.</summary>
/// <param name="Kind">The report.</param>
/// <param name="ReadScope">What reading the same numbers on screen requires.</param>
/// <param name="DefaultSpan">Days (or months, when <paramref name="Monthly"/>) when <c>from</c> is absent, as on the screen.</param>
/// <param name="Monthly">Counted in whole months, like the retention screen.</param>
/// <param name="Title">The file name's stem.</param>
/// <param name="DisplayName">The Thai name a printed report and a spreadsheet's sheet carry.</param>
public sealed record ReportDefinition(ReportKind Kind, string ReadScope, int DefaultSpan, bool Monthly, string Title, string DisplayName);

/// <summary>
/// The reports that can be exported, each tied to the dashboard read it comes from, with that
/// read's own default span.
/// <para>
/// <b>An export needs the report's own scope as well as <c>report:export</c>.</b> The contract
/// puts only <c>report:export</c> on the endpoint, but a file of the clinic's revenue is the same
/// data as <c>GET /staff/analytics/revenue</c>, and a role that may not see that screen must not
/// get it as a spreadsheet instead. So exporting staff performance needs <c>analytics:hr</c>,
/// revenue needs <c>analytics:read</c>, and the operational ones need <c>stats:read</c>.
/// </para>
/// </summary>
public static class ReportCatalogue
{
    public static readonly IReadOnlyList<ReportDefinition> All =
    [
        new(ReportKind.Revenue, Scopes.Analytics.Read, GetRevenueAnalyticsHandler.DefaultDays, Monthly: false, "revenue", "รายรับรายวัน"),
        new(ReportKind.Services, Scopes.Analytics.Read, GetServiceAnalyticsHandler.DefaultDays, Monthly: false, "services", "รายได้ต่อบริการ"),
        new(ReportKind.Retention, Scopes.Analytics.Read, GetRetentionAnalyticsHandler.DefaultMonths, Monthly: true, "retention", "ลูกค้าใหม่และกลับมา (รายเดือน)"),
        new(ReportKind.StaffPerformance, Scopes.Analytics.Hr, GetStaffPerformanceHandler.DefaultDays, Monthly: false, "staff-performance", "ผลงานรายบุคคล"),
        new(ReportKind.Appointments, Scopes.Stats.Read, GetAppointmentStatsHandler.DefaultDays, Monthly: false, "appointments", "สถิตินัดหมาย"),
        new(ReportKind.Queue, Scopes.Stats.Read, GetQueueStatsHandler.DefaultDays, Monthly: false, "queue", "สถิติคิว"),
        new(ReportKind.Workload, Scopes.Stats.Read, GetWorkloadStatsHandler.DefaultDays, Monthly: false, "workload", "ภาระงานของสัตวแพทย์"),
    ];

    public static ReportDefinition Of(ReportKind kind) => All.Single(definition => definition.Kind == kind);
}
