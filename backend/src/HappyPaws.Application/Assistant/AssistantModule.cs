using HappyPaws.Application.Assistant.Chat;
using HappyPaws.Application.Assistant.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Assistant;

/// <summary>
/// Registers the website assistant: the loop, its tools and its daily budget. The model behind it is
/// registered by Infrastructure from <c>Assistant:Provider</c>.
/// </summary>
public static class AssistantModule
{
    public static IServiceCollection AddAssistantModule(this IServiceCollection services)
    {
        services.AddScoped<ChatHandler>();
        services.AddScoped<GetAssistantStatusHandler>();

        // Singleton: the budget is a count across every request of the day.
        services.AddSingleton<AssistantBudget>();

        // Every tool is a public read. Adding one here is adding something the assistant can tell
        // anyone on the internet; see IAssistantTool.
        services.AddScoped<IAssistantTool, ClinicInfoTool>();
        services.AddScoped<IAssistantTool, ServicesTool>();
        services.AddScoped<IAssistantTool, AvailabilityTool>();

        return services;
    }
}
