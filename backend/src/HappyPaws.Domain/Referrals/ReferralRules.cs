namespace HappyPaws.Domain.Referrals;

/// <summary>A customer as the referral rules compare them: who they are and how to reach them.</summary>
public sealed record ReferralParty(string CustomerId, string? Phone, string? Email);

/// <summary>
/// The referral rules that need no database (<c>docs/contract-extensions.md § 39</c>). Each has a
/// unit test of its own.
/// </summary>
public static class ReferralRules
{
    /// <summary>
    /// Whether a sign-up with a code qualifies, as a reason from <see cref="ReferralRejectReasons"/>,
    /// or null when it does. In this order: the friend's own code, a friend who was already a
    /// customer, and a friend who shares a phone number or an email address with the person whose
    /// code it is.
    /// </summary>
    /// <param name="referrer">Whose code it is.</param>
    /// <param name="friend">Who signed up with it.</param>
    /// <param name="friendIsNew">
    /// True only when the sign-up created the friend's customer record. A login that already
    /// existed, or a record the counter made earlier and the sign-up linked to, is not new: that
    /// customer may already have visits and bills.
    /// </param>
    public static string? Judge(ReferralParty referrer, ReferralParty friend, bool friendIsNew)
    {
        if (string.Equals(referrer.CustomerId, friend.CustomerId, StringComparison.Ordinal))
        {
            return ReferralRejectReasons.SelfReferral;
        }

        if (!friendIsNew)
        {
            return ReferralRejectReasons.NotNewCustomer;
        }

        return SamePhone(referrer.Phone, friend.Phone) || SameEmail(referrer.Email, friend.Email)
            ? ReferralRejectReasons.SharedContact
            : null;
    }

    /// <summary>
    /// What the person who invited gets for one more rewarded friend this month: the full points
    /// while fewer than <paramref name="monthlyCap"/> friends have earned them points this calendar
    /// month, nothing after that. The friend's own points do not depend on this.
    /// </summary>
    /// <param name="rewardedThisMonth">Friends who already earned this person points this month.</param>
    /// <param name="monthlyCap">The most friends a month that earn points.</param>
    /// <param name="points">The points one friend is worth.</param>
    public static int ReferrerPointsFor(int rewardedThisMonth, int monthlyCap, int points) =>
        rewardedThisMonth < monthlyCap ? points : 0;

    /// <summary>
    /// Two phone numbers are the same when their digits are, comparing the last nine so that
    /// <c>081-234-5678</c> and <c>+66 81 234 5678</c> match. A missing number matches nothing.
    /// </summary>
    public static bool SamePhone(string? first, string? second)
    {
        var a = Digits(first);
        var b = Digits(second);

        if (a.Length == 0 || b.Length == 0)
        {
            return false;
        }

        return a.Length >= 9 && b.Length >= 9 ? a[^9..] == b[^9..] : a == b;
    }

    /// <summary>Two addresses are the same ignoring case and surrounding spaces. A missing one matches nothing.</summary>
    public static bool SameEmail(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first)
        && !string.IsNullOrWhiteSpace(second)
        && string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string Digits(string? phone) => new([.. (phone ?? string.Empty).Where(char.IsAsciiDigit)]);
}
