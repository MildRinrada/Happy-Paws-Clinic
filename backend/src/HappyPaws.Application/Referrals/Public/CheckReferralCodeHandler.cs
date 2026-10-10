using HappyPaws.Application.Clinics;
using HappyPaws.Application.Crm;
using HappyPaws.Application.Customers;
using HappyPaws.Domain.Referrals;

namespace HappyPaws.Application.Referrals.Public;

/// <summary>
/// <c>GET /referrals/{code}</c>. Not in the API reference: <c>docs/contract-extensions.md § 39</c>.
/// <para>
/// <b>Valid or not, and nothing more.</b> The website's <c>/r/{code}</c> page asks before carrying
/// the code into the booking form. The answer never names the customer the code belongs to, says
/// nothing about their visits, and is the same for a code that never existed, one whose owner left,
/// and any code while the programme is off. Rate-limited per address.
/// </para>
/// </summary>
public sealed class CheckReferralCodeHandler(
    IReferralRepository referrals,
    ICustomerRepository customers,
    IClinicSettingsStore settings)
{
    public async Task<ReferralCodeCheckDto> HandleAsync(string? code, CancellationToken cancellationToken = default)
    {
        if (ReferralCode.Normalize(code) is not { } normalized)
        {
            return new ReferralCodeCheckDto(false);
        }

        if (!(await settings.LoyaltyAsync(cancellationToken)).ReferralEnabled)
        {
            return new ReferralCodeCheckDto(false);
        }

        var valid = await referrals.OwnerOfCodeAsync(normalized, cancellationToken) is { } owner
            && await customers.FindByIdAsync(owner, cancellationToken) is { IsActive: true, IsMerged: false };

        return new ReferralCodeCheckDto(valid);
    }
}
