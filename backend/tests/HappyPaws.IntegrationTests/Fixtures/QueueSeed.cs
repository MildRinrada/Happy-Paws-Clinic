using HappyPaws.Domain.Queueing;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Drives a ticket into a given state without going through the endpoint that normally produces it.
/// <para>
/// All three of these states now have routes — <c>in_room</c> and <c>served</c> from the
/// Consultation module, <c>left</c> from <c>POST /staff/queue/{id}/leave</c>. What this fixture buys
/// is <b>isolation</b>: a board or position-count test that had to open and close a real
/// consultation would fail for reasons that have nothing to do with the queue.
/// </para>
/// </summary>
public static class QueueSeed
{
    public static Task MarkLeftAsync(ApiFixture fixture, string ticketId, CancellationToken cancellationToken) =>
        MutateAsync(fixture, ticketId, ticket => ticket.MarkLeft(DateTimeOffset.UtcNow), cancellationToken);

    /// <summary>Drives a ticket to <c>in_room</c>, the way opening a consultation will.</summary>
    public static Task EnterRoomAsync(ApiFixture fixture, string ticketId, CancellationToken cancellationToken) =>
        MutateAsync(
            fixture,
            ticketId,
            ticket =>
            {
                if (ticket.Status == QueueTicketStatus.Waiting)
                {
                    ticket.Call("stf_seed", DateTimeOffset.UtcNow);
                }

                ticket.EnterRoom(DateTimeOffset.UtcNow);
            },
            cancellationToken);

    /// <summary>Drives a ticket through the room to <c>served</c>, the way closing a consultation does.</summary>
    public static Task MarkServedAsync(ApiFixture fixture, string ticketId, CancellationToken cancellationToken) =>
        MutateAsync(
            fixture,
            ticketId,
            ticket =>
            {
                if (ticket.Status == QueueTicketStatus.Waiting)
                {
                    ticket.Call("stf_seed", DateTimeOffset.UtcNow);
                }

                if (ticket.Status == QueueTicketStatus.Called)
                {
                    ticket.EnterRoom(DateTimeOffset.UtcNow);
                }

                ticket.MarkServed(DateTimeOffset.UtcNow);
            },
            cancellationToken);

    /// <summary>Reads a ticket back outside the request's change tracker.</summary>
    public static async Task<QueueTicket?> ReloadAsync(
        ApiFixture fixture,
        string ticketId,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<QueueTicket>().AsNoTracking()
            .FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);
    }

    private static async Task MutateAsync(
        ApiFixture fixture,
        string ticketId,
        Action<QueueTicket> mutate,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var ticket = await db.Set<QueueTicket>().SingleAsync(t => t.Id == ticketId, cancellationToken);

        mutate(ticket);

        await db.SaveChangesAsync(cancellationToken);
    }
}
