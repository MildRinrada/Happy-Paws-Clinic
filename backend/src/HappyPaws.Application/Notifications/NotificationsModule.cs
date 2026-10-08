using HappyPaws.Application.Notifications.BroadcastToSegment;
using HappyPaws.Application.Notifications.GetDeliveryLog;
using HappyPaws.Application.Notifications.ListMyNotifications;
using HappyPaws.Application.Notifications.ListTemplates;
using HappyPaws.Application.Notifications.SendNotification;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Notifications;

/// <summary>Registers the customer's web inbox, staff sends and broadcasts, the template list and the delivery log.</summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<ListMyNotificationsHandler>();
        services.AddScoped<MarkNotificationsReadHandler>();
        services.AddScoped<SendNotificationHandler>();
        services.AddScoped<ListTemplatesHandler>();
        services.AddScoped<GetDeliveryLogHandler>();
        services.AddScoped<BroadcastToSegmentHandler>();

        return services;
    }
}
