using System.Security.Cryptography;
using System.Text;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Crm;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Crm;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Crm;

internal sealed class CouponRepository(HappyPawsDbContext db) : ICouponRepository
{
    public void Add(Coupon coupon) => db.Set<Coupon>().Add(coupon);

    public void AddRedemption(CouponRedemption redemption) => db.Set<CouponRedemption>().Add(redemption);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        db.Set<Coupon>().AnyAsync(coupon => coupon.Code == code, cancellationToken);

    public Task<string?> IdForCodeAsync(string code, CancellationToken cancellationToken = default) =>
        db.Set<Coupon>().AsNoTracking()
            .Where(coupon => coupon.Code == code)
            .Select(coupon => (string?)coupon.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Coupon?> FindTrackedAsync(string couponId, CancellationToken cancellationToken = default) =>
        db.Set<Coupon>().FirstOrDefaultAsync(coupon => coupon.Id == couponId, cancellationToken);

    public Task<int> StandingRedemptionsAsync(string couponId, string customerId, CancellationToken cancellationToken = default) =>
        db.Set<CouponRedemption>().CountAsync(
            redemption => redemption.CouponId == couponId
                && redemption.CustomerId == customerId
                && redemption.ReleasedAt == null,
            cancellationToken);

    public Task<bool> IsRedeemedOnInvoiceAsync(string couponId, string invoiceId, CancellationToken cancellationToken = default) =>
        db.Set<CouponRedemption>().AnyAsync(
            redemption => redemption.CouponId == couponId
                && redemption.InvoiceId == invoiceId
                && redemption.ReleasedAt == null,
            cancellationToken);

    public async Task<IReadOnlyList<CouponRedemption>> OpenRedemptionsForInvoiceTrackedAsync(
        string invoiceId,
        CancellationToken cancellationToken = default) =>
        await db.Set<CouponRedemption>()
            .Where(redemption => redemption.InvoiceId == invoiceId && redemption.ReleasedAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CouponRedemptionRow>> HistoryForCustomerAsync(
        string customerId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await db.Set<CouponRedemption>().AsNoTracking()
            .Where(redemption => redemption.CustomerId == customerId)
            .OrderByDescending(redemption => redemption.RedeemedAt).ThenByDescending(redemption => redemption.Id)
            .Take(limit)
            .Select(redemption => new CouponRedemptionRow(
                redemption,
                db.Set<Invoice>().Where(invoice => invoice.Id == redemption.InvoiceId).Select(invoice => invoice.Number).FirstOrDefault()))
            .ToListAsync(cancellationToken);

    public async Task<CursorPage<Coupon>> ListAsync(
        CouponFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var coupons = db.Set<Coupon>().AsNoTracking();

        // A coupon for every branch is usable at every counter, so everyone's.
        if (filter.VisibleBranchIds is { } visibleIds)
        {
            var visible = visibleIds.ToArray();
            coupons = coupons.Where(coupon => coupon.BranchId == null || visible.Contains(coupon.BranchId));
        }

        if (filter.BranchId is { Length: > 0 } branchId)
        {
            coupons = coupons.Where(coupon => coupon.BranchId == null || coupon.BranchId == branchId);
        }

        if (filter.Search is { Length: > 0 } search)
        {
            var pattern = $"%{search.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
            coupons = coupons.Where(coupon => EF.Functions.ILike(coupon.Code, pattern) || EF.Functions.ILike(coupon.Name, pattern));
        }

        // The same order of precedence as Coupon.StatusOn, so a filtered row shows the status it was filtered by.
        var today = filter.Today;
        coupons = filter.Status switch
        {
            CouponStatus.Deactivated => coupons.Where(coupon => coupon.DeactivatedAt != null),
            CouponStatus.Expired => coupons.Where(coupon =>
                coupon.DeactivatedAt == null && coupon.ValidUntil != null && coupon.ValidUntil < today),
            CouponStatus.UsedUp => coupons.Where(coupon =>
                coupon.DeactivatedAt == null && (coupon.ValidUntil == null || coupon.ValidUntil >= today)
                && coupon.MaxRedemptions != null && coupon.RedeemedCount >= coupon.MaxRedemptions),
            CouponStatus.Scheduled => coupons.Where(coupon =>
                coupon.DeactivatedAt == null && (coupon.ValidUntil == null || coupon.ValidUntil >= today)
                && (coupon.MaxRedemptions == null || coupon.RedeemedCount < coupon.MaxRedemptions)
                && coupon.ValidFrom > today),
            CouponStatus.Active => coupons.Where(coupon =>
                coupon.DeactivatedAt == null && (coupon.ValidUntil == null || coupon.ValidUntil >= today)
                && (coupon.MaxRedemptions == null || coupon.RedeemedCount < coupon.MaxRedemptions)
                && coupon.ValidFrom <= today),
            _ => coupons,
        };

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            coupons = coupons.Where(coupon =>
                coupon.CreatedAt < sortKey || (coupon.CreatedAt == sortKey && string.Compare(coupon.Id, id) < 0));
        }

        var rows = await coupons
            .OrderByDescending(coupon => coupon.CreatedAt).ThenByDescending(coupon => coupon.Id)
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        return NewestFirst.Page(rows, page, coupon => coupon.CreatedAt, coupon => coupon.Id);
    }
}

internal sealed class LoyaltyLedger(HappyPawsDbContext db) : ILoyaltyLedger
{
    public void Add(LoyaltyEntry entry) => db.Set<LoyaltyEntry>().Add(entry);

    public async Task<IReadOnlyList<LoyaltyEntry>> ForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var saved = await db.Set<LoyaltyEntry>().AsNoTracking()
            .Where(entry => entry.InvoiceId == invoiceId)
            .ToListAsync(cancellationToken);

        // Rows added earlier in this same transaction are not in the database yet.
        var pending = db.ChangeTracker.Entries<LoyaltyEntry>()
            .Where(entry => entry.State == EntityState.Added && entry.Entity.InvoiceId == invoiceId)
            .Select(entry => entry.Entity);

        return [.. saved, .. pending];
    }

    /// <summary>
    /// Reads the customer's rows and works the position out in <see cref="LoyaltyBalance"/>: with
    /// expiry, which points are left depends on the order they were added and spent in, which a
    /// SUM cannot say. A customer's ledger is a few rows a visit, so reading it whole is cheap.
    /// </summary>
    public async Task<LoyaltyPosition> TotalsAsync(
        string customerId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var entries = await db.Set<LoyaltyEntry>().AsNoTracking()
            .Where(entry => entry.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        return LoyaltyBalance.Compute(entries, now);
    }

    public async Task<IReadOnlyList<LoyaltyEntryRow>> HistoryAsync(
        string customerId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await db.Set<LoyaltyEntry>().AsNoTracking()
            .Where(entry => entry.CustomerId == customerId)
            .OrderByDescending(entry => entry.OccurredAt).ThenByDescending(entry => entry.Id)
            .Take(limit)
            .Select(entry => new LoyaltyEntryRow(
                entry,
                db.Set<Invoice>().Where(invoice => invoice.Id == entry.InvoiceId).Select(invoice => invoice.Number).FirstOrDefault()))
            .ToListAsync(cancellationToken);
}

internal sealed class SegmentRepository(HappyPawsDbContext db) : ISegmentRepository
{
    public void Add(Segment segment) => db.Set<Segment>().Add(segment);

    public Task<Segment?> FindByIdAsync(string segmentId, CancellationToken cancellationToken = default) =>
        db.Set<Segment>().AsNoTracking().FirstOrDefaultAsync(segment => segment.Id == segmentId, cancellationToken);

    public async Task<CursorPage<Segment>> ListAsync(
        string? branchId,
        IReadOnlyCollection<string>? visibleBranchIds,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var segments = db.Set<Segment>().AsNoTracking();

        // A group over every branch counts customers a branch-bound account cannot see.
        if (visibleBranchIds is not null)
        {
            var visible = visibleBranchIds.ToArray();
            segments = segments.Where(segment => segment.BranchId != null && visible.Contains(segment.BranchId));
        }

        if (branchId is { Length: > 0 })
        {
            segments = segments.Where(segment => segment.BranchId == branchId);
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            segments = segments.Where(segment =>
                segment.CreatedAt < sortKey || (segment.CreatedAt == sortKey && string.Compare(segment.Id, id) < 0));
        }

        var rows = await segments
            .OrderByDescending(segment => segment.CreatedAt).ThenByDescending(segment => segment.Id)
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        return NewestFirst.Page(rows, page, segment => segment.CreatedAt, segment => segment.Id);
    }
}

/// <summary>Cuts a newest-first read of one row past the page into a page and the cursor for the next.</summary>
internal static class NewestFirst
{
    public static CursorPage<T> Page<T>(
        IReadOnlyList<T> rows,
        CursorPageRequest page,
        Func<T, DateTimeOffset> sortKey,
        Func<T, string> id)
    {
        if (rows.Count <= page.SafeLimit)
        {
            return new CursorPage<T>(rows, null);
        }

        var data = rows.Take(page.SafeLimit).ToList();
        var last = data[^1];

        return new CursorPage<T>(data, Cursor.Encode(sortKey(last), id(last)));
    }
}

/// <summary>
/// Transaction-scoped advisory locks, keyed and salted like the billing and stock locks so they
/// cannot collide with them in PostgreSQL's single lock namespace. Released on commit or rollback.
/// </summary>
internal sealed class CrmLocks(HappyPawsDbContext db) : ICrmLocks
{
    public Task LockCustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        LockAsync($"crm-customer:{customerId}", cancellationToken);

    public Task LockCouponAsync(string couponId, CancellationToken cancellationToken = default) =>
        LockAsync($"crm-coupon:{couponId}", cancellationToken);

    private async Task LockAsync(string name, CancellationToken cancellationToken)
    {
        var key = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(name)), 0);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", [key], cancellationToken);
    }
}
