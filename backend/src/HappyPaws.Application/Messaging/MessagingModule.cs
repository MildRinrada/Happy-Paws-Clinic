using HappyPaws.Application.Messaging.ListThreads;
using HappyPaws.Application.Messaging.ReceiveMessage;
using HappyPaws.Application.Messaging.ReplyToThread;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Messaging;

/// <summary>Registers the customers' threads: the staff inbox, replies, and the inbound path the webhooks use.</summary>
public static class MessagingModule
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services)
    {
        services.AddScoped<ListThreadsHandler>();
        services.AddScoped<ReplyToThreadHandler>();
        services.AddScoped<ReceiveCustomerMessageHandler>();

        return services;
    }
}
