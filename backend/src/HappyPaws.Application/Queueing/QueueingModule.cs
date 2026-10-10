using HappyPaws.Application.Queueing.AssignTicket;
using HappyPaws.Application.Queueing.CallTicket;
using HappyPaws.Application.Queueing.Displays;
using HappyPaws.Application.Queueing.GetMyTicket;
using HappyPaws.Application.Queueing.GetQueueBoard;
using HappyPaws.Application.Queueing.IssueWalkInTicket;
using HappyPaws.Application.Queueing.LeaveQueue;
using HappyPaws.Application.Queueing.SetTicketPriority;
using HappyPaws.Application.Queueing.SkipTicket;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Queueing;

/// <summary>
/// Registers the Queue use cases — the contract's six REST endpoints plus the SSE stream, whose
/// subscription is served straight from <see cref="IQueueEventStream"/> and needs no handler of its
/// own, plus the two contract extensions (leave, priority) described in
/// <c>docs/contract-extensions.md</c>.
/// </summary>
public static class QueueingModule
{
    public static IServiceCollection AddQueueingModule(this IServiceCollection services)
    {
        services.AddScoped<QueueAccess>();
        services.AddScoped<TicketVetGuard>();

        // Waiting-room screens (docs/contract-extensions.md § 30).
        services.AddScoped<QueueDisplayAuthenticator>();
        services.AddScoped<CreateQueueDisplayHandler>();
        services.AddScoped<ListQueueDisplaysHandler>();
        services.AddScoped<RevokeQueueDisplayHandler>();
        services.AddScoped<ClaimQueueDisplayHandler>();
        services.AddScoped<GetDisplayBoardHandler>();

        services.AddScoped<GetQueueBoardHandler>();
        services.AddScoped<IssueWalkInTicketHandler>();
        services.AddScoped<AssignTicketHandler>();
        services.AddScoped<CallTicketHandler>();
        services.AddScoped<SkipTicketHandler>();
        services.AddScoped<LeaveQueueHandler>();
        services.AddScoped<SetTicketPriorityHandler>();
        services.AddScoped<GetMyTicketHandler>();

        return services;
    }
}
