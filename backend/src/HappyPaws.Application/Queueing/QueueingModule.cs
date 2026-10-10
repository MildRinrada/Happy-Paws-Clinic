using HappyPaws.Application.Queueing.AssignTicket;
using HappyPaws.Application.Queueing.CallTicket;
using HappyPaws.Application.Queueing.Displays;
using HappyPaws.Application.Queueing.GetMyTicket;
using HappyPaws.Application.Queueing.GetQueueBoard;
using HappyPaws.Application.Queueing.IssueWalkInTicket;
using HappyPaws.Application.Queueing.LeaveQueue;
using HappyPaws.Application.Queueing.SetTicketPriority;
using HappyPaws.Application.Queueing.SkipTicket;
using HappyPaws.Application.Queueing.WaitEstimate;
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

        // The public walk-in wait estimate (docs/contract-extensions.md § 37). The cache is a
        // singleton because it is the last answer per branch across every request.
        services.AddSingleton<WaitEstimateCache>();
        services.AddScoped<GetWaitEstimateHandler>();

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
