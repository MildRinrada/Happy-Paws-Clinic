namespace HappyPaws.Api.Contracts.PrivacyPolicies;

/// <summary>
/// <c>POST /staff/privacy-policy/versions</c>: where the new draft's text comes from:
/// <code>
/// { "source": "template" }
/// { "source": "current" }
/// { "source": "version", "version_id": "ppv_…" }
/// </code>
/// See <c>docs/contract-extensions.md § 35</c>.
/// </summary>
public sealed record CreatePrivacyPolicyDraftRequest(string? Source = null, string? VersionId = null);

/// <summary>One section on the wire.</summary>
public sealed record PrivacyPolicySectionRequest(string? Anchor = null, string? Heading = null, string? Body = null);

/// <summary>
/// <c>PATCH /staff/privacy-policy/versions/{id}</c>: every section of the draft, in order, which
/// replaces what it had. <c>If-Match</c> is required.
/// <code>
/// { "sections": [ { "anchor": "cookies", "heading": "คุกกี้", "body": "ย่อหน้า\n\n- รายการ\n- [บัญชีของฉัน](/portal/account)" } ] }
/// </code>
/// </summary>
public sealed record UpdatePrivacyPolicyDraftRequest(IReadOnlyList<PrivacyPolicySectionRequest>? Sections = null);

/// <summary>
/// <c>POST /staff/privacy-policy/versions/{id}/publish</c>: <c>If-Match</c> is required:
/// <code>{ "label": "2026-11", "effective_date": "2026-11-01", "legal_review_done": true }</code>
/// </summary>
public sealed record PublishPrivacyPolicyRequest(
    string? Label = null,
    string? EffectiveDate = null,
    bool LegalReviewDone = false);
