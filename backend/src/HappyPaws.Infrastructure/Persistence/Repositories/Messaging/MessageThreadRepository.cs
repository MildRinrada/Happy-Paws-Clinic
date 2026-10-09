using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Messaging;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Messaging;
using HappyPaws.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Messaging;

internal sealed class MessageThreadRepository(HappyPawsDbContext db) : IMessageThreadRepository
{
    public void Add(MessageThread thread) => db.Set<MessageThread>().Add(thread);

    public void AddMessage(ThreadMessage message) => db.Set<ThreadMessage>().Add(message);

    public Task<MessageThread?> FindTrackedAsync(string threadId, CancellationToken cancellationToken = default) =>
        db.Set<MessageThread>().FirstOrDefaultAsync(thread => thread.Id == threadId, cancellationToken);

    public Task<MessageThread?> FindTrackedForCustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        db.Set<MessageThread>().FirstOrDefaultAsync(thread => thread.CustomerId == customerId, cancellationToken);

    public Task<bool> HasInboundAsync(MessageChannel channel, string externalId, CancellationToken cancellationToken = default) =>
        db.Set<ThreadMessage>().AnyAsync(
            message => message.Channel == channel && message.ExternalId == externalId,
            cancellationToken);

    public Task<MessageThread?> FindForCustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        db.Set<MessageThread>().AsNoTracking().FirstOrDefaultAsync(thread => thread.CustomerId == customerId, cancellationToken);

    public Task<string?> LatestBranchOfCustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        db.Set<Appointment>().AsNoTracking()
            .Where(appointment => appointment.CustomerId == customerId)
            .OrderByDescending(appointment => appointment.StartsAt).ThenByDescending(appointment => appointment.Id)
            .Select(appointment => (string?)appointment.BranchId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<CursorPage<ThreadRow>> ListAsync(
        ThreadQuery query,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var threads = db.Set<MessageThread>().AsNoTracking();

        if (query.BranchIds is { } branchIds)
        {
            threads = threads.Where(thread => thread.BranchId == null || branchIds.Contains(thread.BranchId));
        }

        if (query.CustomerId is { } customerId)
        {
            threads = threads.Where(thread => thread.CustomerId == customerId);
        }

        if (query.ThreadId is { } threadId)
        {
            threads = threads.Where(thread => thread.Id == threadId);
        }

        // Mirrors MessageThread.AwaitingReply, in SQL.
        var rows = threads.Select(thread => new
        {
            Thread = thread,
            Awaiting = thread.LastInboundAt != null && (thread.LastReplyAt == null || thread.LastInboundAt > thread.LastReplyAt),
        });

        if (query.AwaitingReply is { } awaiting)
        {
            rows = rows.Where(row => row.Awaiting == awaiting);
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, afterAwaiting, afterId) = DecodeCursor(cursor);

            rows = afterAwaiting
                ? rows.Where(row => !row.Awaiting
                    || row.Thread.LastMessageAt < sortKey
                    || (row.Thread.LastMessageAt == sortKey && string.Compare(row.Thread.Id, afterId) < 0))
                : rows.Where(row => !row.Awaiting
                    && (row.Thread.LastMessageAt < sortKey
                        || (row.Thread.LastMessageAt == sortKey && string.Compare(row.Thread.Id, afterId) < 0)));
        }

        var limit = page.SafeLimit;

        var found = await rows
            .OrderByDescending(row => row.Awaiting)
            .ThenByDescending(row => row.Thread.LastMessageAt)
            .ThenByDescending(row => row.Thread.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = found.Count > limit;
        var data = hasMore ? found[..limit] : found;

        if (data.Count == 0)
        {
            return CursorPage<ThreadRow>.Empty;
        }

        var ids = data.Select(row => row.Thread.Id).ToList();
        var customerIds = data.Select(row => row.Thread.CustomerId).Distinct().ToList();

        var names = await db.Set<Customer>().AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .Select(customer => new { customer.Id, customer.DisplayName })
            .ToDictionaryAsync(customer => customer.Id, customer => customer.DisplayName, cancellationToken);

        var counts = await (
                from message in db.Set<ThreadMessage>()
                join thread in db.Set<MessageThread>() on message.ThreadId equals thread.Id
                where ids.Contains(message.ThreadId)
                group new { message, thread } by message.ThreadId into grouped
                select new
                {
                    ThreadId = grouped.Key,
                    Total = grouped.Count(),
                    Unread = grouped.Sum(item =>
                        item.message.Direction == MessageDirection.Inbound
                        && (item.thread.LastReplyAt == null || item.message.SentAt > item.thread.LastReplyAt)
                            ? 1
                            : 0),
                })
            .ToDictionaryAsync(item => item.ThreadId, cancellationToken);

        var latest = await db.Set<ThreadMessage>().AsNoTracking()
            .Where(message => ids.Contains(message.ThreadId))
            .GroupBy(message => message.ThreadId)
            .Select(grouped => grouped.OrderByDescending(message => message.SentAt).ThenByDescending(message => message.Id).First())
            .ToDictionaryAsync(message => message.ThreadId, cancellationToken);

        return new CursorPage<ThreadRow>(
            [
                .. data.Select(row => new ThreadRow(
                    row.Thread,
                    names.GetValueOrDefault(row.Thread.CustomerId),
                    counts.TryGetValue(row.Thread.Id, out var count) ? count.Unread : 0,
                    count?.Total ?? 0,
                    latest.GetValueOrDefault(row.Thread.Id))),
            ],
            hasMore ? EncodeCursor(data[^1].Thread.LastMessageAt, data[^1].Awaiting, data[^1].Thread.Id) : null);
    }

    public async Task<CursorPage<ThreadMessageRow>> MessagesAsync(
        string threadId,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var messages = db.Set<ThreadMessage>().AsNoTracking().Where(message => message.ThreadId == threadId);

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sentAt, afterId) = Cursor.Decode(cursor);

            messages = messages.Where(message =>
                message.SentAt < sentAt
                || (message.SentAt == sentAt && string.Compare(message.Id, afterId) < 0));
        }

        var limit = page.SafeLimit;

        var rows = await messages
            .OrderByDescending(message => message.SentAt).ThenByDescending(message => message.Id)
            .Take(limit + 1)
            .Select(message => new
            {
                Message = message,
                Name = (
                    from staff in db.Set<StaffMembership>()
                    join user in db.Set<UserAccount>() on staff.UserId equals user.Id
                    where staff.Id == message.SentByStaffId
                    select user.DisplayName).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var data = hasMore ? rows[..limit] : rows;

        return new CursorPage<ThreadMessageRow>(
            [.. data.Select(row => new ThreadMessageRow(row.Message, row.Name))],
            hasMore ? Cursor.Encode(data[^1].Message.SentAt, data[^1].Message.Id) : null);
    }

    public async Task<IReadOnlyList<ThreadMessageRow>> RecentMessagesAsync(
        string threadId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.Set<ThreadMessage>().AsNoTracking()
            .Where(message => message.ThreadId == threadId)
            .OrderByDescending(message => message.SentAt).ThenByDescending(message => message.Id)
            .Take(limit)
            .Select(message => new
            {
                Message = message,
                Name = (
                    from staff in db.Set<StaffMembership>()
                    join user in db.Set<UserAccount>() on staff.UserId equals user.Id
                    where staff.Id == message.SentByStaffId
                    select user.DisplayName).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new ThreadMessageRow(row.Message, row.Name))];
    }

    public Task<string?> StaffNameAsync(string staffId, CancellationToken cancellationToken = default) =>
        (from staff in db.Set<StaffMembership>().AsNoTracking()
         join user in db.Set<UserAccount>() on staff.UserId equals user.Id
         where staff.Id == staffId
         select (string?)user.DisplayName)
        .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// The page key is the thread's last activity plus its id, with whether it was awaiting a reply in
    /// front of the id: awaiting threads come first, so a page boundary has to know which part it is in.
    /// </summary>
    private static string EncodeCursor(DateTimeOffset lastMessageAt, bool awaiting, string id) =>
        Cursor.Encode(lastMessageAt, $"{(awaiting ? '1' : '0')}:{id}");

    private static (DateTimeOffset SortKey, bool Awaiting, string Id) DecodeCursor(string cursor)
    {
        var (sortKey, raw) = Cursor.Decode(cursor);

        if (raw.Length < 3 || raw[1] != ':' || raw[0] is not ('0' or '1'))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]> { ["cursor"] = ["cursor ไม่ถูกต้อง"] });
        }

        return (sortKey, raw[0] == '1', raw[2..]);
    }
}
