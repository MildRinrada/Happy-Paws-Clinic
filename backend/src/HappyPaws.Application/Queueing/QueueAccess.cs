using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing;

/// <summary>
/// Loads a ticket and decides who may act on it.
/// <para>
/// Staff are constrained by branch, exactly as they are on the appointment board — holding
/// <c>queue:call</c> does not mean calling patients at a site you do not work at. Customers reach
/// only their own ticket, and are refused with <c>404</c> rather than <c>403</c>: queue ids are
/// short-lived and guessable in bulk, and confirming which ones exist would expose how busy the
/// clinic is and who is in it.
/// </para>
/// </summary>
public sealed class QueueAccess(
    IQueueTicketRepository tickets,
    ICurrentUser currentUser,
    BranchGuard branchGuard)
{
    /// <summary>Loads a ticket for a staff caller, or throws.</summary>
    public async Task<QueueTicket> ForStaffAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await tickets.FindByIdAsync(ticketId, cancellationToken)
            ?? throw new NotFoundException("queue ticket", ticketId);

        branchGuard.EnsureCanAccess(ticket.BranchId);

        return ticket;
    }

    /// <summary>Loads the caller's own ticket, or throws <c>404</c>.</summary>
    public async Task<QueueTicket> ForOwnerAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        var ticket = await tickets.FindByIdAsync(ticketId, cancellationToken);

        return ticket is not null && ticket.CustomerId == customerId
            ? ticket
            : throw new NotFoundException("queue ticket", ticketId);
    }
}
