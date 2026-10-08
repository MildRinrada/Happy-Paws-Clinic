using HappyPaws.Application.Integrations.ConfigureLineChannel;
using HappyPaws.Application.Integrations.HandleLineWebhook;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Integrations;

/// <summary>Registers the LINE channel settings and the LINE webhook.</summary>
public static class IntegrationsModule
{
    public static IServiceCollection AddIntegrationsModule(this IServiceCollection services)
    {
        services.AddScoped<ConfigureLineChannelHandler>();
        services.AddScoped<HandleLineWebhookHandler>();
        services.AddScoped<LinePostbackExecutor>();

        return services;
    }
}
