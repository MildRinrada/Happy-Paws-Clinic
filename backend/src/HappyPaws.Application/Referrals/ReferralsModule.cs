using HappyPaws.Application.Referrals.Portal;
using HappyPaws.Application.Referrals.Public;
using HappyPaws.Application.Referrals.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Referrals;

/// <summary>
/// Registers refer a friend (<c>docs/contract-extensions.md § 39</c>): the customer's own code and
/// counts, the public code check, the staff list, the recorder the sign-up paths call, and the
/// rewarder the payment path calls.
/// </summary>
public static class ReferralsModule
{
    public static IServiceCollection AddReferralsModule(this IServiceCollection services)
    {
        services.AddScoped<ReferralRecorder>();
        services.AddScoped<ReferralRewarder>();
        services.AddScoped<GetMyReferralHandler>();
        services.AddScoped<CheckReferralCodeHandler>();
        services.AddScoped<ListReferralsHandler>();

        return services;
    }
}
