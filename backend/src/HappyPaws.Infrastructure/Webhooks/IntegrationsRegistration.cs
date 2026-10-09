using HappyPaws.Application.Integrations;
using HappyPaws.Application.Integrations.ApiKeys;
using HappyPaws.Application.Integrations.GetCalendarFeed;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Infrastructure.Persistence.Repositories.Integrations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Webhooks;

/// <summary>
/// Registers the "Webhooks and Integrations" adapters: the secret protector and its key ring, the
/// webhook sender and its locked-down HTTP client, and the repositories.
/// </summary>
public static class IntegrationsRegistration
{
    public const string DataProtectionApplicationName = "HappyPaws.Api";

    public static IServiceCollection AddIntegrationAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- Secrets at rest ----
        // One application name, so several API instances sharing a key directory can read each
        // other's ciphertext. Without a configured directory the framework default is used, which
        // in a container is lost with the container; see docs/development.md.
        var dataProtection = services.AddDataProtection().SetApplicationName(DataProtectionApplicationName);

        if (configuration["DataProtection:KeysDirectory"] is { Length: > 0 } keysDirectory)
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
        }

        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

        // ---- Network rules for webhooks (and the lab URL check) ----
        services.AddOptions<WebhookOptions>().Bind(configuration.GetSection(WebhookOptions.SectionName));

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<WebhookOptions>>().Value;
            var development = provider.GetService<IHostEnvironment>()?.IsDevelopment() ?? false;

            return new IntegrationPolicy(
                AllowLocalhost: development && options.AllowLocalhostInDevelopment,
                Timeout: TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 30)));
        });

        services.AddSingleton<IWebhookAddressResolver, DnsWebhookAddressResolver>();
        services.AddSingleton<WebhookAddressGuard>();

        services.AddHttpClient(WebhookHttpSender.ClientName)
            .ConfigurePrimaryHttpMessageHandler(provider => WebhookHttpHandler.Create(
                provider.GetRequiredService<WebhookAddressGuard>(),
                provider.GetRequiredService<IntegrationPolicy>()));

        services.AddScoped<IWebhookSender, WebhookHttpSender>();

        // ---- Persistence ----
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<ICalendarFeedRepository, CalendarFeedRepository>();

        return services;
    }
}
