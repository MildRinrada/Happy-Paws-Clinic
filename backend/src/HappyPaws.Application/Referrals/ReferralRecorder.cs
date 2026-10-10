using HappyPaws.Application.Clinics;
using HappyPaws.Application.Crm;
using HappyPaws.Application.Customers;
using HappyPaws.Domain.Referrals;

namespace HappyPaws.Application.Referrals;

/// <summary>
/// Records a sign-up that came with a referral code: the optional <c>referral_code</c> on
/// <c>POST /booking/guest/verify</c> and <c>POST /auth/register</c>
/// (<c>docs/contract-extensions.md § 39</c>).
/// <para>
/// <b>Never in the way of the sign-up.</b> A code that is malformed, unknown, belongs to a customer
/// who is no longer active, or arrives while the clinic has the programme switched off is ignored
/// without a word, and so is a customer who was referred before. The answer the friend gets is the
/// same whatever happened here: it never says whose code it was or whether it counted.
/// </para>
/// <para>
/// <b>Rejections are recorded too.</b> A code used by its own owner, by someone who was already a
/// customer, or by someone sharing a phone number or email with the owner is written as a
/// rejected referral with the reason, so the clinic sees codes being tried. Runs inside the
/// caller's transaction, after the friend's customer record exists.
/// </para>
/// </summary>
public sealed class ReferralRecorder(
    IReferralRepository referrals,
    ICustomerRepository customers,
    IClinicSettingsStore settings)
{
    /// <param name="code">What the friend sent, as typed. Null or empty does nothing.</param>
    /// <param name="friend">The customer who signed up.</param>
    /// <param name="friendIsNew">True only when this sign-up created the friend's customer record.</param>
    /// <param name="channel">See <see cref="ReferralChannels"/>.</param>
    /// <param name="now">When.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    public async Task RecordAsync(
        string? code,
        ReferralParty friend,
        bool friendIsNew,
        string channel,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (ReferralCode.Normalize(code) is not { } normalized)
        {
            return;
        }

        if (!(await settings.LoyaltyAsync(cancellationToken)).ReferralEnabled)
        {
            return;
        }

        if (await referrals.OwnerOfCodeAsync(normalized, cancellationToken) is not { } referrerId
            || await customers.FindByIdAsync(referrerId, cancellationToken) is not { IsActive: true, IsMerged: false } referrer)
        {
            return;
        }

        if (await referrals.ExistsForFriendAsync(friend.CustomerId, cancellationToken))
        {
            return;
        }

        var reason = ReferralRules.Judge(new ReferralParty(referrer.Id, referrer.Phone, referrer.Email), friend, friendIsNew);

        referrals.Add(Referral.Record(referrer.Id, friend.CustomerId, normalized, channel, reason, now));
    }
}
