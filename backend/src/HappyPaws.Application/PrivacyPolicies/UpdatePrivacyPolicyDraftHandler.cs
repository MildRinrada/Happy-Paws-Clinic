using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.Application.PrivacyPolicies;

/// <summary>
/// <c>PATCH /staff/privacy-policy/versions/{id}</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 35</c>.
/// </summary>
/// <param name="VersionId">From the path.</param>
/// <param name="Sections">Every section, in order. Replaces what the draft had. Null changes nothing.</param>
/// <param name="IfMatchVersion">Required: the draft's <c>version</c>.</param>
public sealed record UpdatePrivacyPolicyDraftCommand(
    string VersionId,
    IReadOnlyList<PrivacyPolicySection>? Sections,
    uint? IfMatchVersion = null);

/// <summary>
/// Rewrites a draft. A published version is fixed for good (<c>409 state_conflict</c>): a consent
/// points at its label, and the words behind that label must be the ones the customer was shown.
/// <para>
/// <c>If-Match</c> is <b>required</b>, as for articles: this is long text two people may have
/// open, and a blind save would throw away the other person's paragraphs. The sections are
/// checked before anything is loaded (<c>422</c>, the field is the section's path, e.g.
/// <c>sections[2].body</c>). Audited as <c>privacy_policy.draft_updated</c> with counts, not the
/// text; a request that changes nothing writes nothing.
/// </para>
/// </summary>
public sealed class UpdatePrivacyPolicyDraftHandler(
    IPrivacyPolicyRepository policies,
    IAuditTrail audit,
    IUnitOfWork unitOfWork)
{
    public async Task<StaffPrivacyPolicyVersionDto> HandleAsync(
        UpdatePrivacyPolicyDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PrivacyPolicySection>? cleaned = null;

        if (command.Sections is { } sections)
        {
            cleaned = PrivacyPolicyRules.Clean(sections);

            if (PrivacyPolicyRules.Problems(cleaned) is { Count: > 0 } problems)
            {
                throw new ValidationFailedException(problems);
            }
        }

        PrivacyPolicyVersion? draft = null;
        string? currentId = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            draft = await policies.FindAsync(command.VersionId, token)
                ?? throw new NotFoundException("privacy_policy_version", command.VersionId);

            // Published first: no version of the request could make that edit acceptable.
            if (draft.IsPublished)
            {
                throw new StateConflictException("privacy_policy_version", "published", "edit");
            }

            ConcurrencyGuard.EnsureRequired(draft, command.IfMatchVersion);

            currentId = (await policies.CurrentAsync(token))?.Id;

            if (cleaned is null || cleaned.SequenceEqual(draft.Sections))
            {
                return;
            }

            var before = (Count: draft.Sections.Count, Length: draft.Sections.Sum(section => section.Body.Length));

            draft.ReplaceSections(cleaned);

            audit.Record(new AuditEvent(
                AuditActions.PrivacyPolicyDraftUpdated,
                AuditResources.PrivacyPolicy,
                draft.Id,
                Details: new Dictionary<string, object?>
                {
                    ["section_count_before"] = before.Count,
                    ["section_count_after"] = draft.Sections.Count,
                    ["body_length_before"] = before.Length,
                    ["body_length_after"] = draft.Sections.Sum(section => section.Body.Length),
                    ["anchors"] = draft.Sections.Select(section => section.Anchor).ToArray(),
                }));
        }, cancellationToken);

        return StaffPrivacyPolicyVersionDto.From(draft!, currentId);
    }
}
