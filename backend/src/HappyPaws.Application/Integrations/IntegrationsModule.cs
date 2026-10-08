using HappyPaws.Application.Integrations.ConfigureLineChannel;
using HappyPaws.Application.Integrations.HandleLineWebhook;
using HappyPaws.Application.Integrations.ListLineFollowers;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Integrations;

/// <summary>
/// Registers the LINE channel settings, the LINE webhook, and the recent follower list of
/// <c>docs/contract-extensions.md § 18</c>.
/// </summary>
public static class IntegrationsModule
{
    public static IServiceCollection AddIntegrationsModule(this IServiceCollection services)
    {
        services.AddScoped<ConfigureLineChannelHandler>();
        services.AddScoped<HandleLineWebhookHandler>();
        services.AddScoped<LinePostbackExecutor>();
        services.AddScoped<ListLineFollowersHandler>();

        return services;
    }
}
