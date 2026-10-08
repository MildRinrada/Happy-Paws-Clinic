using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Integrations;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Integrations;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Integrations;

internal sealed class LineFollowerRepository(HappyPawsDbContext db) : ILineFollowerRepository
{
    public void Add(LineFollower follower) => db.Set<LineFollower>().Add(follower);

    public Task<LineFollower?> FindTrackedAsync(string lineUserId, CancellationToken cancellationToken = default) =>
        db.Set<LineFollower>().FirstOrDefaultAsync(follower => follower.LineUserId == lineUserId, cancellationToken);

    public Task<LineFollower?> FindAsync(string lineUserId, CancellationToken cancellationToken = default) =>
        db.Set<LineFollower>().AsNoTracking().FirstOrDefaultAsync(follower => follower.LineUserId == lineUserId, cancellationToken);

    public async Task<CursorPage<LineFollowerRow>> ListAsync(
        LineFollowerQuery query,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        // The address belongs to whichever record carries it, merged ones included: the unique
        // index on customers.line_user_id covers them too, so such an address cannot be linked again.
        var rows = db.Set<LineFollower>().AsNoTracking()
            .Where(follower => follower.UnfollowedAt == null && follower.LastSeenAt >= query.SeenSince)
            .Select(follower => new
            {
                Follower = follower,
                CustomerId = db.Set<Customer>()
                    .Where(customer => customer.LineUserId == follower.LineUserId)
                    .Select(customer => customer.Id)
                    .FirstOrDefault(),
            });

        if (query.UnlinkedOnly)
        {
            rows = rows.Where(row => row.CustomerId == null);
        }

        if (query.Text is { Length: > 0 } text)
        {
            var pattern = $"%{text}%";
            rows = rows.Where(row => row.Follower.DisplayName != null && EF.Functions.ILike(row.Follower.DisplayName, pattern));
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);

            rows = rows.Where(row =>
                row.Follower.LastSeenAt < sortKey
                || (row.Follower.LastSeenAt == sortKey && string.Compare(row.Follower.Id, id) < 0));
        }

        var limit = page.SafeLimit;

        var found = await rows
            .OrderByDescending(row => row.Follower.LastSeenAt).ThenByDescending(row => row.Follower.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = found.Count > limit;
        var data = hasMore ? found[..limit] : found;

        return new CursorPage<LineFollowerRow>(
            [.. data.Select(row => new LineFollowerRow(row.Follower, row.CustomerId))],
            hasMore ? Cursor.Encode(data[^1].Follower.LastSeenAt, data[^1].Follower.Id) : null);
    }
}
