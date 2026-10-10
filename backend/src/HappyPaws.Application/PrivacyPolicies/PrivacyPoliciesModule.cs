using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.PrivacyPolicies;

/// <summary>
/// Registers the clinic's own privacy notice: the two public reads and the staff desk for drafts
/// and publishing (<c>docs/contract-extensions.md § 35</c>).
/// </summary>
public static class PrivacyPoliciesModule
{
    public static IServiceCollection AddPrivacyPoliciesModule(this IServiceCollection services)
    {
        services.AddScoped<GetPublicPrivacyPolicyHandler>();
        services.AddScoped<ListPrivacyPolicyVersionsHandler>();
        services.AddScoped<CreatePrivacyPolicyDraftHandler>();
        services.AddScoped<UpdatePrivacyPolicyDraftHandler>();
        services.AddScoped<PublishPrivacyPolicyHandler>();

        return services;
    }
}
