using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm;

/// <summary>Persistence port for customers' packages and the uses spent from them.</summary>
public interface ICustomerPackageRepository
{
    void Add(CustomerPackage package);

    void AddUse(PackageUse use);

    /// <summary>Every package a customer has bought, newest first. Not tracked.</summary>
    Task<IReadOnlyList<CustomerPackage>> ForCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The customer's paid, uncancelled packages, tracked, for spending a use. Call under
    /// <see cref="ICrmLocks.LockCustomerAsync"/>.
    /// </summary>
    Task<IReadOnlyList<CustomerPackage>> SpendableForCustomerTrackedAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>The packages sold on a bill, tracked.</summary>
    Task<IReadOnlyList<CustomerPackage>> SoldOnInvoiceTrackedAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>The uses a bill spent and has not given back, tracked.</summary>
    Task<IReadOnlyList<PackageUse>> OpenUsesForInvoiceTrackedAsync(string invoiceId, CancellationToken cancellationToken = default);

    Task<CustomerPackage?> FindTrackedAsync(string packageId, CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for coupons and their redemptions.</summary>
public interface ICouponRepository
{
    void Add(Coupon coupon);

    void AddRedemption(CouponRedemption redemption);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>The coupon's id for a normalized code, or null. A plain read, taken before the lock.</summary>
    Task<string?> IdForCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Read fresh and tracked. Call under <see cref="ICrmLocks.LockCouponAsync"/>.</summary>
    Task<Coupon?> FindTrackedAsync(string couponId, CancellationToken cancellationToken = default);

    /// <summary>Uses of this coupon by this customer that a void has not released.</summary>
    Task<int> StandingRedemptionsAsync(string couponId, string customerId, CancellationToken cancellationToken = default);

    Task<bool> IsRedeemedOnInvoiceAsync(string couponId, string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>Redemptions on a bill that a void has not released, tracked.</summary>
    Task<IReadOnlyList<CouponRedemption>> OpenRedemptionsForInvoiceTrackedAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>A customer's redemptions, newest first, with the bill number.</summary>
    Task<IReadOnlyList<CouponRedemptionRow>> HistoryForCustomerAsync(string customerId, int limit, CancellationToken cancellationToken = default);

    /// <summary>One page of coupons, newest first. Untracked. See <see cref="CouponFilter"/>.</summary>
    Task<CursorPage<Coupon>> ListAsync(CouponFilter filter, CursorPageRequest page, CancellationToken cancellationToken = default);
}

/// <summary>
/// What <c>GET /staff/coupons</c> narrows by (<c>docs/contract-extensions.md § 25</c>).
/// <para>
/// <c>BranchId</c> means "the coupons a bill at this branch could take": those for the branch and
/// those for every branch. <c>VisibleBranchIds</c> applies the caller's own branches the same way,
/// in the query, so paging cannot pass it.
/// </para>
/// </summary>
/// <param name="Today">The clinic day the status is judged on.</param>
/// <param name="Status">Only coupons in this state, or null for all.</param>
/// <param name="BranchId">Coupons usable at this branch, or null.</param>
/// <param name="Search">Part of the code or the name.</param>
/// <param name="VisibleBranchIds">The caller's branches, or null when not branch-bound.</param>
public sealed record CouponFilter(
    DateOnly Today,
    CouponStatus? Status = null,
    string? BranchId = null,
    string? Search = null,
    IReadOnlyCollection<string>? VisibleBranchIds = null);

/// <summary>A redemption as the loyalty page lists it.</summary>
public sealed record CouponRedemptionRow(CouponRedemption Redemption, string? InvoiceNumber);

/// <summary>The points ledger. Append-only.</summary>
public interface ILoyaltyLedger
{
    void Add(LoyaltyEntry entry);

    /// <summary>The ledger rows written for one bill.</summary>
    Task<IReadOnlyList<LoyaltyEntry>> ForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Spendable points as of <paramref name="now"/>, lifetime points (earned less reversed), and
    /// what has expired or expires next. See <see cref="LoyaltyBalance"/>.
    /// </summary>
    Task<LoyaltyPosition> TotalsAsync(string customerId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>The latest rows, newest first, with the bill number.</summary>
    Task<IReadOnlyList<LoyaltyEntryRow>> HistoryAsync(string customerId, int limit, CancellationToken cancellationToken = default);
}

public sealed record LoyaltyEntryRow(LoyaltyEntry Entry, string? InvoiceNumber);

public interface ISegmentRepository
{
    void Add(Segment segment);

    Task<Segment?> FindByIdAsync(string segmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of saved groups, newest first. Untracked. With <paramref name="visibleBranchIds"/>,
    /// only groups limited to one of those branches; with <paramref name="branchId"/>, only that
    /// branch's groups.
    /// </summary>
    Task<CursorPage<Segment>> ListAsync(
        string? branchId,
        IReadOnlyCollection<string>? visibleBranchIds,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Works out who is in a customer group, in SQL. One query shape serves both the count shown when
/// the group is created and the list a broadcast sends to, so the two cannot disagree.
/// </summary>
public interface ISegmentQuery
{
    /// <summary>Members today, and how many of them a LINE broadcast could reach.</summary>
    Task<SegmentCounts> CountAsync(SegmentCriteria criteria, DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>
    /// The members who agreed to marketing and have a LINE address that still follows the clinic,
    /// at most <paramref name="limit"/> of them.
    /// </summary>
    Task<IReadOnlyList<string>> ReachableMembersAsync(
        SegmentCriteria criteria,
        DateOnly today,
        int limit,
        CancellationToken cancellationToken = default);
}

public sealed record SegmentCounts(int Customers, int Reachable);

/// <summary>
/// Transaction-scoped locks that serialise the CRM writes that check a number before changing it:
/// a customer's points and package uses, and one coupon's count.
/// </summary>
public interface ICrmLocks
{
    Task LockCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    Task LockCouponAsync(string couponId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Which catalogue service each line of a saved bill is for: the treatment's service on a case
/// bill, or the service id itself on a counter bill. The same derivation the service revenue report
/// uses.
/// </summary>
public interface IBilledServiceLookup
{
    Task<IReadOnlyDictionary<string, string>> ServiceIdsByLineAsync(string invoiceId, CancellationToken cancellationToken = default);
}
