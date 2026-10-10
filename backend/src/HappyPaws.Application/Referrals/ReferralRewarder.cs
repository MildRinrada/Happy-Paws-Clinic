using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Crm;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Crm;
using HappyPaws.Domain.Referrals;

namespace HappyPaws.Application.Referrals;

/// <summary>
/// Pays a referral when the friend's first bill is paid (<c>docs/contract-extensions.md § 39</c>).
/// Called by <see cref="InvoiceRewards.PaymentCollectedAsync"/>, inside the payment's transaction,
/// so the bill, the points and the referral land together or not at all.
/// <para>
/// <b>Once.</b> Only a pending referral pays, and paying it closes it. The friend's lock is taken
/// first, so two payments of the friend's bills at the same moment pay one of them; the unique
/// referral key on the points ledger is the backstop. A later refund takes back points the bill
/// earned, never referral points: the friend did come and pay, and the ledger's reversals follow
/// money on a bill, which referral points are not.
/// </para>
/// <para>
/// <b>The cap is on the person who invited.</b> Under the referrer's lock, so two friends paying
/// at once cannot both be the fifth: friends who already earned this person points this calendar
/// month (clinic time) are counted, and past the cap the referral is still rewarded, the friend
/// still gets their points, and the referrer gets none, marked <c>over_monthly_cap</c>.
/// </para>
/// <para>
/// <b>Switched off.</b> A pending referral whose friend pays while the programme is off is closed
/// as rejected with <c>programme_off</c>, so turning the programme on later does not pay for a
/// visit made while it was off.
/// </para>
/// </summary>
public sealed class ReferralRewarder(
    IReferralRepository referrals,
    ILoyaltyLedger ledger,
    ICrmLocks locks,
    IClinicSettingsStore settings,
    IAuditTrail audit,
    ClinicDays clinicDays)
{
    /// <summary>A note on each ledger row. Says nothing about the other person.</summary>
    public const string ReferrerNote = "แต้มชวนเพื่อน";

    public const string FriendNote = "แต้มต้อนรับจากการชวนของเพื่อน";

    /// <summary>The bill reached <c>paid</c>. Does nothing unless it is the friend's first with money on it.</summary>
    public async Task InvoicePaidAsync(Invoice invoice, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (invoice.Status != InvoiceStatus.Paid || invoice.Paid.Satang <= 0)
        {
            return;
        }

        // Nearly every bill belongs to someone with nothing pending: they pay without taking a lock.
        if (!await referrals.HasPendingForFriendAsync(invoice.CustomerId, cancellationToken))
        {
            return;
        }

        // Read again under the lock: a second payment that also saw "pending" waits here, then
        // reads the reward the first one committed and does nothing.
        await locks.LockCustomerAsync(invoice.CustomerId, cancellationToken);

        if (await referrals.PendingForFriendTrackedAsync(invoice.CustomerId, cancellationToken) is not { } referral)
        {
            return;
        }

        var loyalty = await settings.LoyaltyAsync(cancellationToken);

        if (!loyalty.ReferralEnabled)
        {
            referral.Reject(ReferralRejectReasons.ProgrammeOff, now);
            return;
        }

        await locks.LockCustomerAsync(referral.ReferrerCustomerId, cancellationToken);

        var (monthStart, monthEnd) = MonthOf(now);
        var paidThisMonth = await referrals.PaidToReferrerBetweenAsync(referral.ReferrerCustomerId, monthStart, monthEnd, cancellationToken);
        var referrerPoints = ReferralRules.ReferrerPointsFor(paidThisMonth, loyalty.ReferralMonthlyCap, loyalty.ReferralReferrerPoints);
        var overCap = paidThisMonth >= loyalty.ReferralMonthlyCap;
        var friendPoints = loyalty.ReferralFriendPoints;

        referral.Reward(invoice.Id, referrerPoints, friendPoints, overCap, now);

        var expiresAt = loyalty.ExpiryFor(now);

        if (referrerPoints > 0)
        {
            ledger.Add(LoyaltyEntry.Create(
                referral.ReferrerCustomerId, LoyaltyEntryKind.Referral, referrerPoints, now,
                note: ReferrerNote, expiresAt: expiresAt, referralId: referral.Id));
        }

        if (friendPoints > 0)
        {
            ledger.Add(LoyaltyEntry.Create(
                referral.FriendCustomerId, LoyaltyEntryKind.Referral, friendPoints, now,
                note: FriendNote, expiresAt: expiresAt, referralId: referral.Id));
        }

        audit.Record(new AuditEvent(
            AuditActions.ReferralRewarded,
            AuditResources.Referral,
            referral.Id,
            BranchId: invoice.BranchId,
            Details: new Dictionary<string, object?>
            {
                ["referrer_customer_id"] = referral.ReferrerCustomerId,
                ["friend_customer_id"] = referral.FriendCustomerId,
                ["invoice_id"] = invoice.Id,
                ["referrer_points"] = referrerPoints,
                ["friend_points"] = friendPoints,
                ["over_monthly_cap"] = overCap,
            }));
    }

    /// <summary>The clinic calendar month an instant falls in, as <c>[start, end)</c>.</summary>
    private (DateTimeOffset Start, DateTimeOffset End) MonthOf(DateTimeOffset now)
    {
        var day = clinicDays.DateOf(now);
        var first = new DateOnly(day.Year, day.Month, 1);

        return (clinicDays.StartOf(first), clinicDays.StartOf(first.AddMonths(1)));
    }
}
