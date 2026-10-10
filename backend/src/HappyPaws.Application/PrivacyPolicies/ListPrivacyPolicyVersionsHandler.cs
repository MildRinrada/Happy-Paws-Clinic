using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.Application.PrivacyPolicies;

/// <summary>
/// <c>GET /staff/privacy-policy/versions</c> (<c>settings:read</c>). Not in the API reference: see
/// <c>docs/contract-extensions.md § 35</c>.
/// <para>
/// Every version with its text, drafts included, newest made first. A clinic keeps a handful of
/// versions over years, so there is no paging. The answer also says which version is current and
/// whether the template that ships with this release is newer than anything the clinic has made a
/// draft from.
/// </para>
/// </summary>
public sealed class ListPrivacyPolicyVersionsHandler(IPrivacyPolicyRepository policies)
{
    public async Task<StaffPrivacyPolicyListDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var versions = await policies.ListAsync(cancellationToken);
        var currentId = versions
            .Where(version => version.IsPublished)
            .OrderByDescending(version => version.PublishedAt)
            .ThenByDescending(version => version.Id, StringComparer.Ordinal)
            .Select(version => version.Id)
            .FirstOrDefault();

        var newerTemplate = !versions.Any(version => version.TemplateVersion == PrivacyPolicyTemplate.Version);

        return new StaffPrivacyPolicyListDto(
            [.. versions.Select(version => StaffPrivacyPolicyVersionDto.From(version, currentId))],
            currentId,
            new PrivacyPolicyTemplateDto(PrivacyPolicyTemplate.Version, newerTemplate));
    }
}
