using HappyPaws.Application.Common;
using HappyPaws.Domain.Referrals;

namespace HappyPaws.Application.Referrals;

/// <summary>
/// <c>GET /portal/referral</c> (<c>docs/contract-extensions.md § 39</c>): the customer's own code and
/// link, and counts of what came of it. Counts only: no friend is named, so sharing a link never
/// tells its owner who signed up.
/// </summary>
/// <param name="Enabled">Whether the clinic has the programme on. Off, the link still opens the booking page, and nothing is earned.</param>
/// <param name="Code">The customer's code.</param>
/// <param name="Link">The address to share.</param>
/// <param name="Invited">Everyone who signed up with the code, whatever came of it.</param>
/// <param name="Rewarded">Friends whose first bill was paid and who were rewarded.</param>
/// <param name="Pending">Friends who signed up and have not had a paid bill yet.</param>
/// <param name="PointsEarned">Points this customer got from referrals, all time.</param>
/// <param name="MonthlyCap">How many friends a calendar month earn this customer points.</param>
/// <param name="RewardedThisMonth">Friends who earned this customer points this calendar month.</param>
/// <param name="ReferrerPoints">What one rewarded friend earns this customer now.</param>
/// <param name="FriendPoints">What a friend earns on their first paid bill now.</param>
public sealed record MyReferralDto(
    bool Enabled,
    string Code,
    string Link,
    int Invited,
    int Rewarded,
    int Pending,
    int PointsEarned,
    int MonthlyCap,
    int RewardedThisMonth,
    int ReferrerPoints,
    int FriendPoints);

/// <summary><c>GET /referrals/{code}</c>: whether the code can be used. Nothing else, ever.</summary>
public sealed record ReferralCodeCheckDto(bool Valid);

/// <summary>One side of a referral as staff see it.</summary>
public sealed record ReferralPartyDto(string CustomerId, string? Name);

/// <summary>A row of <c>GET /staff/referrals</c>.</summary>
public sealed record StaffReferralDto(
    string Id,
    ReferralPartyDto Referrer,
    ReferralPartyDto Friend,
    string Code,
    string Channel,
    string Status,
    string? RejectReason,
    int ReferrerPoints,
    int FriendPoints,
    bool OverMonthlyCap,
    string? RewardInvoiceId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RewardedAt,
    DateTimeOffset? RejectedAt)
{
    public static StaffReferralDto From(StaffReferralRow row)
    {
        var referral = row.Referral;

        return new StaffReferralDto(
            referral.Id,
            new ReferralPartyDto(referral.ReferrerCustomerId, row.ReferrerName),
            new ReferralPartyDto(referral.FriendCustomerId, row.FriendName),
            referral.Code,
            referral.Channel,
            WireEnum.Of(referral.Status),
            referral.RejectReason,
            referral.ReferrerPoints,
            referral.FriendPoints,
            referral.OverMonthlyCap,
            referral.RewardInvoiceId,
            referral.CreatedAt,
            referral.RewardedAt,
            referral.RejectedAt);
    }
}
