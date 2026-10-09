using HappyPaws.Application.Integrations.ApiKeys;
using HappyPaws.Application.Integrations.ConfigureLabProvider;
using HappyPaws.Application.Integrations.ConfigureLineChannel;
using HappyPaws.Application.Integrations.ConfigurePaymentProvider;
using HappyPaws.Application.Integrations.CreateApiKey;
using HappyPaws.Application.Integrations.CreateWebhookSubscription;
using HappyPaws.Application.Integrations.DispatchWebhooks;
using HappyPaws.Application.Integrations.GetCalendarFeed;
using HappyPaws.Application.Integrations.HandleLineWebhook;
using HappyPaws.Application.Integrations.ListLineFollowers;
using HappyPaws.Application.Integrations.ListWebhookDeliveries;
using HappyPaws.Application.Integrations.Webhooks;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Integrations;

/// <summary>
/// Registers the LINE channel settings, the LINE webhook, the recent follower list of
/// <c>docs/contract-extensions.md § 18</c>, and the "Webhooks and Integrations" section: outbound
/// webhooks with their outbox, API keys, the payment and lab provider settings, and the calendar feed.
/// </summary>
public static class IntegrationsModule
{
    public static IServiceCollection AddIntegrationsModule(this IServiceCollection services)
    {
        services.AddScoped<ConfigureLineChannelHandler>();
        services.AddScoped<HandleLineWebhookHandler>();
        services.AddScoped<LinePostbackExecutor>();
        services.AddScoped<ListLineFollowersHandler>();

        services.AddScoped<IWebhookOutbox, WebhookOutbox>();
        services.AddScoped<CreateWebhookSubscriptionHandler>();
        services.AddScoped<ListWebhookDeliveriesHandler>();
        services.AddScoped<DispatchWebhooksHandler>();
        services.AddScoped<CreateApiKeyHandler>();
        services.AddScoped<ApiKeyAuthenticator>();
        services.AddScoped<ConfigurePaymentProviderHandler>();
        services.AddScoped<ConfigureLabProviderHandler>();
        services.AddScoped<CalendarFeedHandler>();

        return services;
    }
}
