using HappyPaws.Application.Queueing;
using HappyPaws.Domain.Queueing;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Queueing;

/// <summary>
/// EF Core implementation of the queue port.
/// <para>
/// Call order is expressed once, in <see cref="InCallOrder"/>, and reused by the board and by the
/// "how many ahead of me" count. Two hand-written orderings would be one edit away from telling a
/// waiting owner a different story from the screen above their head.
/// </para>
/// </summary>
internal sealed class QueueTicketRepository(HappyPawsDbContext db) : IQueueTicketRepository
{
    public void Add(QueueTicket ticket) => db.Set<QueueTicket>().Add(ticket);

    public Task<QueueTicket?> FindByIdAsync(string ticketId, CancellationToken cancellationToken = default) =>
        db.Set<QueueTicket>().FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);

    public Task<QueueTicket?> FindByAppointmentAsync(
        string appointmentId,
        CancellationToken cancellationToken = default) =>
        db.Set<QueueTicket>()
            .AsNoTracking()
            .FirstOrDefaultAsync(ticket => ticket.AppointmentId == appointmentId, cancellationToken);

    public Task<QueueTicket?> FindTrackedByAppointmentAsync(
        string appointmentId,
        CancellationToken cancellationToken = default) =>
        db.Set<QueueTicket>()
            .FirstOrDefaultAsync(ticket => ticket.AppointmentId == appointmentId, cancellationToken);

    public async Task<IReadOnlyList<QueueTicket>> BoardAsync(
        string branchId,
        DateOnly serviceDate,
        string? roomId = null,
        bool includeFinished = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<QueueTicket>()
            .AsNoTracking()
            .Where(ticket => ticket.BranchId == branchId && ticket.ServiceDate == serviceDate);

        if (!includeFinished)
        {
            var live = QueueTicketTransitions.LiveStatuses;
            query = query.Where(ticket => live.Contains(ticket.Status));
        }

        if (roomId is { Length: > 0 })
        {
            query = query.Where(ticket => ticket.RoomId == roomId);
        }

        return await InCallOrder(query).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QueueTicket>> LiveForCustomerAsync(
        string customerId,
        DateOnly serviceDate,
        CancellationToken cancellationToken = default)
    {
        var live = QueueTicketTransitions.LiveStatuses;

        return await db.Set<QueueTicket>()
            .AsNoTracking()
            .Where(ticket => ticket.CustomerId == customerId && ticket.ServiceDate == serviceDate && live.Contains(ticket.Status))
            .OrderBy(ticket => ticket.IssuedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAheadOfAsync(QueueTicket ticket, CancellationToken cancellationToken = default)
    {
        var awaiting = QueueTicketTransitions.AwaitingCallStatuses;

        // "Ahead" is the same comparison the ordering makes, in the same sequence: urgency first,
        // then unskipped before skipped, then earlier skip, then lower number. Anything else and
        // the number an owner reads on their phone disagrees with the board above their head.
        return await db.Set<QueueTicket>()
            .AsNoTracking()
            .Where(other => other.BranchId == ticket.BranchId
                && other.ServiceDate == ticket.ServiceDate
                && other.Id != ticket.Id
                && awaiting.Contains(other.Status)
                && (other.Priority > ticket.Priority
                    || (other.Priority == ticket.Priority
                        && (ticket.SkippedAt == null
                            ? other.SkippedAt == null && other.Sequence < ticket.Sequence
                            : other.SkippedAt == null
                                || other.SkippedAt < ticket.SkippedAt
                                || (other.SkippedAt == ticket.SkippedAt && other.Sequence < ticket.Sequence)))))
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// The order tickets will be called in.
    /// <para>
    /// Urgency first, then everyone not passed over by number, then everyone who was by when it
    /// happened. "เลื่อนไปท้ายแถว" is expressed as a sort rather than as a renumbering of every
    /// other row, and priority sits in front of it so an emergency arriving mid-morning does not
    /// have to wait behind the people already skipped.
    /// </para>
    /// <para>
    /// Priority is compared as the stored enum, which PostgreSQL orders as text. That is why
    /// <see cref="QueueTicket.Priority"/> is persisted as an integer rather than a name: sorted as
    /// text, <c>Emergency</c> would come before <c>Normal</c> by luck and <c>Urgent</c> after it by
    /// the same luck, and the second one is wrong.
    /// </para>
    /// </summary>
    private static IQueryable<QueueTicket> InCallOrder(IQueryable<QueueTicket> query) =>
        query
            .OrderByDescending(ticket => ticket.Priority)
            .ThenBy(ticket => ticket.SkippedAt == null ? 0 : 1)
            .ThenBy(ticket => ticket.SkippedAt)
            .ThenBy(ticket => ticket.Sequence);
}

/// <summary>
/// Allocates the next queue number by serialising on a PostgreSQL advisory transaction lock keyed
/// to the branch and the day.
/// <para>
/// "Read the highest number and add one" is a lost-update race: two check-ins a millisecond apart
/// both read A013 and both try to write A014. The unique index on
/// <c>(branch_id, service_date, number)</c> would catch that, but only by failing a customer's
/// check-in at the counter — a correct answer delivered as an error.
/// </para>
/// <para>
/// The advisory lock makes the second caller wait a few milliseconds instead. It is scoped to the
/// transaction (<c>pg_advisory_xact_lock</c>), so it is released on commit or rollback with no
/// unlock call to forget, and it is keyed per branch per day, so two branches never queue behind
/// each other. The unique index stays as the backstop.
/// </para>
/// </summary>
internal sealed class QueueNumberGenerator(HappyPawsDbContext db) : IQueueNumberGenerator
{
    public async Task<int> NextAsync(
        string branchId,
        DateOnly serviceDate,
        bool resetDaily,
        CancellationToken cancellationToken = default)
    {
        // A sequence that never resets is one sequence per branch, so it is locked per branch:
        // two check-ins either side of midnight must not both read the same highest number.
        var key = LockKey(branchId, resetDaily ? serviceDate : null);

        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            [key],
            cancellationToken);

        var tickets = db.Set<QueueTicket>().Where(ticket => ticket.BranchId == branchId);

        if (resetDaily)
        {
            tickets = tickets.Where(ticket => ticket.ServiceDate == serviceDate);
        }

        // Max rather than count: a ticket deleted by a data fix must not cause the next one to
        // reuse a number that has already been printed and called out.
        var highest = await tickets.MaxAsync(ticket => (int?)ticket.Sequence, cancellationToken);

        return (highest ?? 0) + 1;
    }

    /// <summary>
    /// A stable 64-bit key for the branch and, when numbering resets daily, the day. Advisory locks
    /// are a single global namespace of integers, so the hash is what keeps this from colliding
    /// with an unrelated lock — and it is derived deterministically so every process picks the same
    /// number for the same sequence.
    /// </summary>
    private static long LockKey(string branchId, DateOnly? serviceDate)
    {
        var value = serviceDate is { } day ? $"queue:{branchId}:{day:yyyy-MM-dd}" : $"queue:{branchId}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));

        return BitConverter.ToInt64(hash, 0);
    }
}
