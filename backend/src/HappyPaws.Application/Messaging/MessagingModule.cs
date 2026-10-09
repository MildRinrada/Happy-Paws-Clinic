using HappyPaws.Application.Messaging.ListThreads;
using HappyPaws.Application.Messaging.Portal;
using HappyPaws.Application.Messaging.ReceiveMessage;
using HappyPaws.Application.Messaging.ReplyToThread;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Messaging;

/// <summary>
/// Registers the customers' threads: the staff inbox, replies, the inbound path the webhooks use,
/// and the customer's own chat in the portal (<c>docs/contract-extensions.md § 24</c>).
/// </summary>
public static class MessagingModule
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services)
    {
        services.AddScoped<ListThreadsHandler>();
        services.AddScoped<ReplyToThreadHandler>();
        services.AddScoped<ReceiveCustomerMessageHandler>();
        services.AddScoped<GetMyThreadHandler>();
        services.AddScoped<WriteToClinicHandler>();

        return services;
    }
}
