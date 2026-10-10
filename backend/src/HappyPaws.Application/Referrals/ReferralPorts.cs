using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Referrals;

namespace HappyPaws.Application.Referrals;

/// <summary>A referral with the two names the staff list shows beside it.</summary>
/// <param name="Referral">The referral.</param>
/// <param name="ReferrerName">The person whose code was used. Staff only.</param>
/// <param name="FriendName">The person who signed up with it. Staff only.</param>
public sealed record StaffReferralRow(Referral Referral, string? ReferrerName, string? FriendName);

/// <summary>What one person's referrals add up to, for their own portal page. Counts only.</summary>
/// <param name="Invited">Everyone who signed up with the code, whatever came of it.</param>
/// <param name="Rewarded">Friends whose first bill was paid and who were rewarded.</param>
/// <param name="Pending">Friends who signed up and have not had a paid bill yet.</param>
/// <param name="PointsEarned">Points this person got from referrals, all time.</param>
/// <param name="RewardedThisMonth">Friends who earned this person points this calendar month.</param>
public sealed record ReferralTotals(int Invited, int Rewarded, int Pending, int PointsEarned, int RewardedThisMonth);

/// <summary>Persistence port for referral codes and referrals (<c>docs/contract-extensions.md § 39</c>).</summary>
public interface IReferralRepository
{
    /// <summary>
    /// The customer's code, making it first when they have none. Saved at once, outside any
    /// transaction of the caller's: two first reads at the same moment get the same code, and a
    /// new code that happens to equal someone else's is drawn again.
    /// </summary>
    Task<string> EnsureCodeAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>The customer a normalised code belongs to, or null.</summary>
    Task<string?> OwnerOfCodeAsync(string code, CancellationToken cancellationToken = default);

    void Add(Referral referral);

    /// <summary>True when the customer was already referred, whatever came of it.</summary>
    Task<bool> ExistsForFriendAsync(string friendCustomerId, CancellationToken cancellationToken = default);

    /// <summary>True when the customer has a pending referral. A plain read, taken before the lock.</summary>
    Task<bool> HasPendingForFriendAsync(string friendCustomerId, CancellationToken cancellationToken = default);

    /// <summary>The customer's referral, tracked for change, when it is still pending. Call under the friend's lock.</summary>
    Task<Referral?> PendingForFriendTrackedAsync(string friendCustomerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many of this person's referrals earned them points (more than zero) with a reward time in
    /// <c>[from, to)</c>.
    /// </summary>
    Task<int> PaidToReferrerBetweenAsync(
        string referrerCustomerId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>One person's totals, with "this month" as <c>[monthStart, monthEnd)</c>.</summary>
    Task<ReferralTotals> TotalsForReferrerAsync(
        string referrerCustomerId,
        DateTimeOffset monthStart,
        DateTimeOffset monthEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Referrals newest first, all of them or those where the customer is either side. Untracked.
    /// </summary>
    Task<CursorPage<StaffReferralRow>> ListAsync(
        string? customerId,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}

/// <summary>The link a customer shares, from configuration.</summary>
public interface IReferralLinks
{
    /// <summary>The public address for one code, e.g. <c>https://happypaws.co.th/r/7KX4PM2Q</c>.</summary>
    string ForCode(string code);
}
