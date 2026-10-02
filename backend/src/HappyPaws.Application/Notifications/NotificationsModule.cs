using HappyPaws.Application.Notifications.ListMyNotifications;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Notifications;

/// <summary>Registers the customer's web inbox.</summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<ListMyNotificationsHandler>();
        services.AddScoped<MarkNotificationsReadHandler>();

        return services;
    }
}
