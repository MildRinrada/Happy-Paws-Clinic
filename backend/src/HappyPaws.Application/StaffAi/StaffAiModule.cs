using HappyPaws.Application.StaffAi.Ask;
using HappyPaws.Application.StaffAi.DailyBrief;
using HappyPaws.Application.StaffAi.OwnerMessage;
using HappyPaws.Application.StaffAi.PetSummary;
using HappyPaws.Application.StaffAi.SoapDraft;
using HappyPaws.Application.StaffAi.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.StaffAi;

/// <summary>
/// Registers the staff AI assistant (<c>docs/contract-extensions.md § 34</c>). Its model, its
/// settings and its stores are registered by Infrastructure from <c>StaffAi:*</c>.
/// </summary>
public static class StaffAiModule
{
    public static IServiceCollection AddStaffAiModule(this IServiceCollection services)
    {
        services.AddScoped<StaffAiQuota>();
        services.AddScoped<StaffAiGateway>();

        services.AddScoped<SummarizePetHandler>();
        services.AddScoped<DraftSoapHandler>();
        services.AddScoped<DraftOwnerMessageHandler>();
        services.AddScoped<AskHandler>();
        services.AddScoped<GenerateDailyBriefsHandler>();
        services.AddScoped<GetDailyBriefHandler>();
        services.AddScoped<GetStaffAiUsageHandler>();

        // Each tool is an existing report's use case, offered only to a caller holding its scope.
        services.AddScoped<IStaffAiTool, TodayDashboardTool>();
        services.AddScoped<IStaffAiTool, AppointmentStatsTool>();
        services.AddScoped<IStaffAiTool, QueueStatsTool>();
        services.AddScoped<IStaffAiTool, WorkloadStatsTool>();
        services.AddScoped<IStaffAiTool, RevenueTool>();
        services.AddScoped<IStaffAiTool, ServiceAnalyticsTool>();
        services.AddScoped<IStaffAiTool, RetentionTool>();
        services.AddScoped<IStaffAiTool, StaffPerformanceTool>();
        services.AddScoped<IStaffAiTool, InventoryAlertsTool>();

        return services;
    }
}
