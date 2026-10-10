using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Crm;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Referrals.Portal;

/// <summary>
/// <c>GET /portal/referral</c>. Not in the API reference: <c>docs/contract-extensions.md § 39</c>.
/// <para>
/// The customer comes from the session. The code is made the first time this is read and never
/// changes after. The counts say how many friends signed up and what came of it, never who they
/// are: a friend's booking is theirs, and the person who shared the link sees numbers only.
/// </para>
/// </summary>
public sealed class GetMyReferralHandler(
    IReferralRepository referrals,
    IReferralLinks links,
    IClinicSettingsStore settings,
    ICurrentUser currentUser,
    ClinicDays clinicDays)
{
    public async Task<MyReferralDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();
        var code = await referrals.EnsureCodeAsync(customerId, cancellationToken);
        var loyalty = await settings.LoyaltyAsync(cancellationToken);

        var today = clinicDays.Today;
        var first = new DateOnly(today.Year, today.Month, 1);
        var totals = await referrals.TotalsForReferrerAsync(
            customerId, clinicDays.StartOf(first), clinicDays.StartOf(first.AddMonths(1)), cancellationToken);

        return new MyReferralDto(
            loyalty.ReferralEnabled,
            code,
            links.ForCode(code),
            totals.Invited,
            totals.Rewarded,
            totals.Pending,
            totals.PointsEarned,
            loyalty.ReferralMonthlyCap,
            totals.RewardedThisMonth,
            loyalty.ReferralReferrerPoints,
            loyalty.ReferralFriendPoints);
    }
}
