using HappyPaws.Application.Analytics.GetAppointmentStats;
using HappyPaws.Application.Analytics.GetQueueStats;
using HappyPaws.Application.Analytics.GetRetentionAnalytics;
using HappyPaws.Application.Analytics.GetRevenueAnalytics;
using HappyPaws.Application.Analytics.GetServiceAnalytics;
using HappyPaws.Application.Analytics.GetStaffPerformance;
using HappyPaws.Application.Analytics.GetTodayDashboard;
using HappyPaws.Application.Analytics.GetWorkloadStats;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Analytics;

/// <summary>Registers the operational statistics and business dashboard reads.</summary>
public static class AnalyticsModule
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddScoped<ReportWindows>();

        services.AddScoped<GetTodayDashboardHandler>();
        services.AddScoped<GetAppointmentStatsHandler>();
        services.AddScoped<GetQueueStatsHandler>();
        services.AddScoped<GetWorkloadStatsHandler>();
        services.AddScoped<GetRevenueAnalyticsHandler>();
        services.AddScoped<GetServiceAnalyticsHandler>();
        services.AddScoped<GetRetentionAnalyticsHandler>();
        services.AddScoped<GetStaffPerformanceHandler>();

        return services;
    }
}
