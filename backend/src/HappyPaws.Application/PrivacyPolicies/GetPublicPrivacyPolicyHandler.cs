using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.Application.PrivacyPolicies;

/// <summary>
/// <c>GET /privacy-policy</c> and <c>GET /privacy-policy/versions/{label}</c>. Not in the API
/// reference: see <c>docs/contract-extensions.md § 35</c>.
/// <para>
/// Only ever published text. The current notice answers <c>200</c> with <c>published: false</c>
/// when the clinic has not published one yet, never the template: the template is the developer's
/// words, and showing it would be the clinic saying something it never agreed to say. A label that
/// no published version has, a draft's id included, is <c>404</c>.
/// </para>
/// </summary>
public sealed class GetPublicPrivacyPolicyHandler(IPrivacyPolicyRepository policies)
{
    public async Task<PublicPrivacyPolicyDto> CurrentAsync(CancellationToken cancellationToken = default)
    {
        var current = await policies.CurrentAsync(cancellationToken);

        if (current is null)
        {
            return PublicPrivacyPolicyDto.NotPublished;
        }

        var history = await policies.ListPublishedAsync(cancellationToken);

        return PublicPrivacyPolicyDto.From(current, isCurrent: true, history);
    }

    public async Task<PublicPrivacyPolicyDto> ByLabelAsync(string label, CancellationToken cancellationToken = default)
    {
        var wanted = label.Trim();

        if (!PrivacyPolicyRules.IsLabel(wanted))
        {
            throw new NotFoundException("privacy_policy_version", wanted);
        }

        var version = await policies.FindPublishedAsync(wanted, cancellationToken)
            ?? throw new NotFoundException("privacy_policy_version", wanted);

        var history = await policies.ListPublishedAsync(cancellationToken);

        return PublicPrivacyPolicyDto.From(version, isCurrent: history.Count > 0 && history[0].Label == version.Label, history);
    }
}
