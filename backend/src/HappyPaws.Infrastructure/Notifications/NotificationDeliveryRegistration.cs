using HappyPaws.Application.Integrations;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Infrastructure.Notifications.Email;
using HappyPaws.Infrastructure.Notifications.Line;
using HappyPaws.Infrastructure.Notifications.Routing;
using HappyPaws.Infrastructure.Notifications.Sms;
using HappyPaws.Infrastructure.Persistence.Repositories.Integrations;
using HappyPaws.Infrastructure.Persistence.Repositories.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Infrastructure.Notifications;

/// <summary>
/// Wires customer message delivery: the LINE, SMS and email routes behind one
/// <see cref="INotificationChannel"/>, the delivery log, and the LINE gateway the webhook and the
/// settings use. Depends on <c>AddMessaging</c> for the SMS and email transports.
/// </summary>
internal static class NotificationDeliveryRegistration
{
    public static IServiceCollection AddNotificationDelivery(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LineOptions>().Bind(configuration.GetSection(LineOptions.SectionName));
        services.AddHttpClient(LineMessagingClient.ClientName, client => client.Timeout = TimeSpan.FromSeconds(10));

        services.AddSingleton<LineMessagingClient>();
        services.AddSingleton<LoggingNotificationChannel>();
        services.AddSingleton<IDeliveryTransports, ConfiguredDeliveryTransports>();
        services.AddSingleton<NotificationComposer>();

        services.AddScoped<IDeliveryLog, DeliveryLogRepository>();
        services.AddScoped<NotificationRecipientReader>();
        services.AddScoped<LineCredentialResolver>();
        services.AddScoped<ILineGateway, LineGateway>();
        services.AddScoped<ILineAddressBook, LineAddressBook>();
        services.AddScoped<ILineFollowerRepository, LineFollowerRepository>();

        // Registration order is not routing order; DeliveringNotificationChannel orders them.
        services.AddScoped<IDeliveryRoute, LineDeliveryRoute>();
        services.AddScoped<IDeliveryRoute, SmsDeliveryRoute>();
        services.AddScoped<IDeliveryRoute, EmailDeliveryRoute>();

        // Scoped, because LINE's switches and credentials are read from the database per request.
        services.AddScoped<INotificationChannel, DeliveringNotificationChannel>();

        return services;
    }
}
