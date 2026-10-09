using System.Security.Cryptography;
using System.Text;
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

    public async Task<LoyaltyTotals> TotalsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var totals = await db.Set<LoyaltyEntry>().AsNoTracking()
            .Where(entry => entry.CustomerId == customerId)
            .GroupBy(entry => entry.CustomerId)
            .Select(group => new
            {
                Balance = group.Sum(entry => entry.Points),
                Lifetime = group
                    .Where(entry => entry.Kind == LoyaltyEntryKind.Earn || entry.Kind == LoyaltyEntryKind.Reverse)
                    .Sum(entry => entry.Points),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return totals is null ? new LoyaltyTotals(0, 0) : new LoyaltyTotals(totals.Balance, totals.Lifetime);
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
