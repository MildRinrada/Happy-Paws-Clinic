using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Referrals;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Referrals;
using HappyPaws.Infrastructure.Persistence.Configurations.Referrals;
using HappyPaws.Infrastructure.Persistence.Repositories.Crm;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Referrals;

/// <summary>Referral codes and referrals (<c>docs/contract-extensions.md § 39</c>).</summary>
internal sealed class ReferralRepository(HappyPawsDbContext db) : IReferralRepository
{
    /// <summary>PostgreSQL <c>unique_violation</c>.</summary>
    private const string UniqueViolation = "23505";

    /// <summary>A drawn code equal to someone else's is drawn again; this many tries is far past any real collision.</summary>
    private const int CodeAttempts = 5;

    public async Task<string> EnsureCodeAsync(string customerId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < CodeAttempts; attempt++)
        {
            if (await CodeOfAsync(customerId, cancellationToken) is { } existing)
            {
                return existing;
            }

            var code = ReferralCode.For(customerId);
            var entry = db.Set<ReferralCode>().Add(code);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return code.Code;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            {
                SqlState: UniqueViolation,
                ConstraintName: ReferralCodeConfiguration.CustomerIndex or ReferralCodeConfiguration.CodeIndex,
            })
            {
                // Either another request made this customer's code first, which the next read
                // returns, or the drawn code is someone else's, and the next try draws again.
                entry.State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("Could not allocate a referral code.");
    }

    public Task<string?> OwnerOfCodeAsync(string code, CancellationToken cancellationToken = default) =>
        db.Set<ReferralCode>().AsNoTracking()
            .Where(row => row.Code == code)
            .Select(row => row.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(Referral referral) => db.Set<Referral>().Add(referral);

    public Task<bool> ExistsForFriendAsync(string friendCustomerId, CancellationToken cancellationToken = default) =>
        db.Set<Referral>().AsNoTracking().AnyAsync(referral => referral.FriendCustomerId == friendCustomerId, cancellationToken);

    public Task<bool> HasPendingForFriendAsync(string friendCustomerId, CancellationToken cancellationToken = default) =>
        db.Set<Referral>().AsNoTracking().AnyAsync(
            referral => referral.FriendCustomerId == friendCustomerId && referral.Status == ReferralStatus.Pending,
            cancellationToken);

    public Task<Referral?> PendingForFriendTrackedAsync(string friendCustomerId, CancellationToken cancellationToken = default) =>
        db.Set<Referral>().FirstOrDefaultAsync(
            referral => referral.FriendCustomerId == friendCustomerId && referral.Status == ReferralStatus.Pending,
            cancellationToken);

    public Task<int> PaidToReferrerBetweenAsync(
        string referrerCustomerId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        db.Set<Referral>().AsNoTracking().CountAsync(
            referral => referral.ReferrerCustomerId == referrerCustomerId
                && referral.Status == ReferralStatus.Rewarded
                && referral.ReferrerPoints > 0
                && referral.RewardedAt >= from
                && referral.RewardedAt < to,
            cancellationToken);

    public async Task<ReferralTotals> TotalsForReferrerAsync(
        string referrerCustomerId,
        DateTimeOffset monthStart,
        DateTimeOffset monthEnd,
        CancellationToken cancellationToken = default)
    {
        // One customer's referrals are a handful of rows; counting them in memory keeps the four
        // numbers in one read.
        var rows = await db.Set<Referral>().AsNoTracking()
            .Where(referral => referral.ReferrerCustomerId == referrerCustomerId)
            .Select(referral => new { referral.Status, referral.ReferrerPoints, referral.RewardedAt })
            .ToListAsync(cancellationToken);

        return new ReferralTotals(
            rows.Count,
            rows.Count(row => row.Status == ReferralStatus.Rewarded),
            rows.Count(row => row.Status == ReferralStatus.Pending),
            rows.Sum(row => row.ReferrerPoints),
            rows.Count(row => row.Status == ReferralStatus.Rewarded
                && row.ReferrerPoints > 0
                && row.RewardedAt >= monthStart
                && row.RewardedAt < monthEnd));
    }

    public async Task<CursorPage<StaffReferralRow>> ListAsync(
        string? customerId,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var referrals = db.Set<Referral>().AsNoTracking();

        if (customerId is not null)
        {
            referrals = referrals.Where(referral =>
                referral.ReferrerCustomerId == customerId || referral.FriendCustomerId == customerId);
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            referrals = referrals.Where(referral =>
                referral.CreatedAt < sortKey || (referral.CreatedAt == sortKey && string.Compare(referral.Id, id) < 0));
        }

        var rows = await (
                from referral in referrals
                join referrer in db.Set<Customer>() on referral.ReferrerCustomerId equals referrer.Id into referrers
                from referrer in referrers.DefaultIfEmpty()
                join friend in db.Set<Customer>() on referral.FriendCustomerId equals friend.Id into friends
                from friend in friends.DefaultIfEmpty()
                orderby referral.CreatedAt descending, referral.Id descending
                select new StaffReferralRow(referral, referrer.DisplayName, friend.DisplayName))
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        return NewestFirst.Page(rows, page, row => row.Referral.CreatedAt, row => row.Referral.Id);
    }

    private Task<string?> CodeOfAsync(string customerId, CancellationToken cancellationToken) =>
        db.Set<ReferralCode>().AsNoTracking()
            .Where(row => row.CustomerId == customerId)
            .Select(row => row.Code)
            .FirstOrDefaultAsync(cancellationToken);
}
