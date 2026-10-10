using System.Text.Json;
using HappyPaws.Application.Analytics.GetAppointmentStats;
using HappyPaws.Application.Analytics.GetQueueStats;
using HappyPaws.Application.Analytics.GetRetentionAnalytics;
using HappyPaws.Application.Analytics.GetRevenueAnalytics;
using HappyPaws.Application.Analytics.GetServiceAnalytics;
using HappyPaws.Application.Analytics.GetStaffPerformance;
using HappyPaws.Application.Analytics.GetTodayDashboard;
using HappyPaws.Application.Analytics.GetWorkloadStats;
using HappyPaws.Application.Assistant;
using HappyPaws.Application.Inventory.ListAlerts;
using HappyPaws.Domain.Access;

namespace HappyPaws.Application.StaffAi.Ask;

/// <summary>The arguments every report over a span of days takes.</summary>
internal static class RangeSchema
{
    public const string Properties =
        """
        "from":{"type":"string","description":"First clinic day, YYYY-MM-DD. Omit for the report's default span (the last 30 days)."},
        "to":{"type":"string","description":"Last clinic day included, YYYY-MM-DD. Omit for today."},
        "branch_id":{"type":"string","description":"One branch id from the list in the instructions. Omit for every branch the caller may see."}
        """;

    public const string Granularity = "\"granularity\":{\"type\":\"string\",\"description\":\"day, week or month. Default day.\"}";

    /// <summary>The schema object: the range's properties and any extra ones.</summary>
    public static string Json(params string[] extra) =>
        "{\"type\":\"object\",\"properties\":{" + string.Join(",", [Properties, .. extra]) + "}}";

    public static (DateOnly? From, DateOnly? To, string? Branch) Read(JsonElement arguments) =>
        (StaffToolArguments.Date(arguments, "from"), StaffToolArguments.Date(arguments, "to"), StaffToolArguments.String(arguments, "branch_id"));
}

/// <summary><c>get_today_dashboard</c>: <c>GET /staff/dashboard/today</c>.</summary>
public sealed class TodayDashboardTool(GetTodayDashboardHandler handler) : IStaffAiTool
{
    public string Name => "get_today_dashboard";

    public string Description =>
        "Today's numbers at a glance: appointments by status, the queue (waiting, in room, served, average and longest "
        + "wait), open and closed cases, and the caller's own day. Today's takings are included only when the caller "
        + "may see money.";

    public string ParametersSchema =>
        """{"type":"object","properties":{"branch_id":{"type":"string","description":"One branch id. Omit for every branch the caller may see."}}}""";

    public string RequiredScope => Scopes.Dashboard.Read;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default) =>
        new(
            await handler.HandleAsync(new GetTodayDashboardQuery(StaffToolArguments.String(arguments, "branch_id")), cancellationToken),
            [new AssistantAction("ดูภาพรวมวันนี้", "/dashboard")]);
}

/// <summary><c>get_appointment_stats</c>: <c>GET /staff/stats/appointments</c>.</summary>
public sealed class AppointmentStatsTool(GetAppointmentStatsHandler handler) : IStaffAiTool
{
    public string Name => "get_appointment_stats";

    public string Description =>
        "Appointment volume over a span of clinic days: totals by status, cancellation and no-show rates, walk-ins, "
        + "online bookings, and a series by day, week or month.";

    public string ParametersSchema =>
        RangeSchema.Json(RangeSchema.Granularity);

    public string RequiredScope => Scopes.Stats.Read;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var (from, to, branch) = RangeSchema.Read(arguments);
        var result = await handler.HandleAsync(
            new GetAppointmentStatsQuery(from, to, branch, StaffToolArguments.String(arguments, "granularity")),
            cancellationToken);

        return new ToolResult(result, [new AssistantAction("ดูสถิตินัด", "/dashboard?tab=operations")]);
    }
}

/// <summary><c>get_queue_stats</c>: <c>GET /staff/stats/queue</c>.</summary>
public sealed class QueueStatsTool(GetQueueStatsHandler handler) : IStaffAiTool
{
    public string Name => "get_queue_stats";

    public string Description =>
        "The queue over a span of days: tickets, served, skipped, left, still outstanding, average wait and room "
        + "time, and the peak hours.";

    public string ParametersSchema => RangeSchema.Json();

    public string RequiredScope => Scopes.Stats.Read;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var (from, to, branch) = RangeSchema.Read(arguments);
        return new ToolResult(
            await handler.HandleAsync(new GetQueueStatsQuery(from, to, branch), cancellationToken),
            [new AssistantAction("ดูสถิติคิว", "/dashboard?tab=operations")]);
    }
}

/// <summary><c>get_workload_stats</c>: <c>GET /staff/stats/workload</c>. Staff names reach the model as placeholders.</summary>
public sealed class WorkloadStatsTool(GetWorkloadStatsHandler handler) : IStaffAiTool
{
    public string Name => "get_workload_stats";

    public string Description =>
        "Workload over a span of days: cases per vet, average minutes per case, open cases and waiting queue per "
        + "person. People are named by placeholders such as [STAFF_1].";

    public string ParametersSchema => RangeSchema.Json();

    public string RequiredScope => Scopes.Stats.Read;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var (from, to, branch) = RangeSchema.Read(arguments);
        return new ToolResult(
            await handler.HandleAsync(new GetWorkloadStatsQuery(from, to, branch), cancellationToken),
            [new AssistantAction("ดูภาระงาน", "/dashboard?tab=operations")]);
    }
}

/// <summary><c>get_revenue</c>: <c>GET /staff/analytics/revenue</c>.</summary>
public sealed class RevenueTool(GetRevenueAnalyticsHandler handler) : IStaffAiTool
{
    public string Name => "get_revenue";

    public string Description =>
        "Revenue over a span of clinic days, cash basis: collected, refunded and net in satang (100 satang = 1 baht), "
        + "by payment method, by branch, and a series by day, week or month. Also what was invoiced.";

    public string ParametersSchema =>
        RangeSchema.Json(RangeSchema.Granularity);

    public string RequiredScope => Scopes.Analytics.Read;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var (from, to, branch) = RangeSchema.Read(arguments);
        var result = await handler.HandleAsync(
            new GetRevenueAnalyticsQuery(from, to, branch, StaffToolArguments.String(arguments, "granularity")),
            cancellationToken);

        return new ToolResult(result, [new AssistantAction("ดูรายได้", "/dashboard?tab=business")]);
    }
}

/// <summary><c>get_service_analytics</c>: <c>GET /staff/analytics/services</c>.</summary>
public sealed class ServiceAnalyticsTool(GetServiceAnalyticsHandler handler) : IStaffAiTool
{
    public string Name => "get_service_analytics";

    public string Description =>
        "Which services earned what over a span of days, and the margin per service after medicine costs. Money in satang.";

    public string ParametersSchema => RangeSchema.Json();

    public string RequiredScope => Scopes.Analytics.Read;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var (from, to, branch) = RangeSchema.Read(arguments);
        return new ToolResult(
            await handler.HandleAsync(new GetServiceAnalyticsQuery(from, to, branch), cancellationToken),
            [new AssistantAction("ดูบริการทำเงิน", "/dashboard?tab=business")]);
    }
}

/// <summary><c>get_retention</c>: <c>GET /staff/analytics/retention</c>.</summary>
public sealed class RetentionTool(GetRetentionAnalyticsHandler handler) : IStaffAiTool
{
    public string Name => "get_retention";

    public string Description =>
        "New and returning customers per month and monthly cohorts. Counts only, no names. The span is in whole months, "
        + "six months back by default.";

    public string ParametersSchema => RangeSchema.Json();

    public string RequiredScope => Scopes.Analytics.Read;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var (from, to, branch) = RangeSchema.Read(arguments);
        return new ToolResult(
            await handler.HandleAsync(new GetRetentionAnalyticsQuery(from, to, branch), cancellationToken),
            [new AssistantAction("ดูลูกค้ากลับมา", "/dashboard?tab=business")]);
    }
}

/// <summary><c>get_staff_performance</c>: <c>GET /staff/analytics/staff-performance</c>. Names as placeholders.</summary>
public sealed class StaffPerformanceTool(GetStaffPerformanceHandler handler) : IStaffAiTool
{
    public string Name => "get_staff_performance";

    public string Description =>
        "Cases, revenue and review scores per staff member over a span of days. People are named by placeholders such as [STAFF_1].";

    public string ParametersSchema => RangeSchema.Json();

    public string RequiredScope => Scopes.Analytics.Hr;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var (from, to, branch) = RangeSchema.Read(arguments);
        return new ToolResult(
            await handler.HandleAsync(new GetStaffPerformanceQuery(from, to, branch), cancellationToken),
            [new AssistantAction("ดูผลงานรายคน", "/dashboard?tab=business")]);
    }
}

/// <summary><c>get_inventory_alerts</c>: <c>GET /staff/inventory/alerts</c>, worst first, the first 40.</summary>
public sealed class InventoryAlertsTool(ListAlertsHandler handler) : IStaffAiTool
{
    private const int MaxRows = 40;

    public string Name => "get_inventory_alerts";

    public string Description =>
        "Stock that needs attention, worst first: expired lots still on the shelf, items out of stock, lots near "
        + "expiry and items at or below their reorder level, with units on order.";

    public string ParametersSchema =>
        """
        {"type":"object","properties":{
          "branch_id":{"type":"string","description":"One branch id. Omit for every branch the caller may see."},
          "within_days":{"type":"integer","description":"How far ahead near-expiry looks, 1 to 365. Default 90."}
        }}
        """;

    public string RequiredScope => Scopes.Inventory.Read;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var alerts = await handler.HandleAsync(
            new ListAlertsQuery(StaffToolArguments.String(arguments, "branch_id"), StaffToolArguments.Int(arguments, "within_days")),
            cancellationToken);

        return new ToolResult(
            new { total = alerts.Count, shown = Math.Min(alerts.Count, MaxRows), alerts = alerts.Take(MaxRows) },
            [new AssistantAction("ดูสต๊อกที่ต้องดูแล", "/inventory")]);
    }
}
