using HappyPaws.Application.Common;
using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.Application.PrivacyPolicies;

/// <summary>One section of the notice, as stored (<c>docs/contract-extensions.md § 35</c>).</summary>
/// <param name="Anchor">The part's address on the page, <c>/privacy#cookies</c>.</param>
/// <param name="Heading">One line.</param>
/// <param name="Body">Plain text: blank line, <c>- </c> list items and <c>[label](/path)</c>.</param>
public sealed record PrivacyPolicySectionDto(string Anchor, string Heading, string Body)
{
    public static PrivacyPolicySectionDto From(PrivacyPolicySection section) =>
        new(section.Anchor, section.Heading, section.Body);
}

/// <summary>A published version in the public history list.</summary>
/// <param name="Label">The version's label, for <c>GET /privacy-policy/versions/{label}</c>.</param>
/// <param name="EffectiveDate">"In effect from".</param>
/// <param name="PublishedAt">When it was published.</param>
public sealed record PrivacyPolicyHistoryDto(string Label, DateOnly EffectiveDate, DateTimeOffset PublishedAt)
{
    public static PrivacyPolicyHistoryDto From(PublishedPolicySummary summary) =>
        new(summary.Label, summary.EffectiveDate, summary.PublishedAt);
}

/// <summary>
/// <c>GET /privacy-policy</c> and <c>GET /privacy-policy/versions/{label}</c>. When nothing is
/// published, <c>published</c> is <c>false</c> and only <c>history</c> (empty) is sent: the page then
/// says the clinic has not published its notice yet, rather than showing any text.
/// </summary>
/// <param name="Published">False when the clinic has not published a notice yet.</param>
/// <param name="Label">This version's label.</param>
/// <param name="EffectiveDate">"In effect from".</param>
/// <param name="PublishedAt">When it was published.</param>
/// <param name="LegalReviewDone">The owner's statement that a lawyer reviewed this text.</param>
/// <param name="IsCurrent">True for the current notice; false for an older one read by label.</param>
/// <param name="Sections">The text.</param>
/// <param name="History">Every published version, newest first, so the page can link the older ones.</param>
public sealed record PublicPrivacyPolicyDto(
    bool Published,
    string? Label,
    DateOnly? EffectiveDate,
    DateTimeOffset? PublishedAt,
    bool? LegalReviewDone,
    bool? IsCurrent,
    IReadOnlyList<PrivacyPolicySectionDto>? Sections,
    IReadOnlyList<PrivacyPolicyHistoryDto> History)
{
    public static PublicPrivacyPolicyDto NotPublished { get; } = new(false, null, null, null, null, null, null, []);

    public static PublicPrivacyPolicyDto From(
        PrivacyPolicyVersion version,
        bool isCurrent,
        IReadOnlyList<PublishedPolicySummary> history) => new(
        true,
        version.Label,
        version.EffectiveDate,
        version.PublishedAt,
        version.LegalReviewDone,
        isCurrent,
        [.. version.Sections.Select(PrivacyPolicySectionDto.From)],
        [.. history.Select(PrivacyPolicyHistoryDto.From)]);
}

/// <summary>One version as staff see it, drafts included (<c>docs/contract-extensions.md § 35</c>).</summary>
/// <param name="Id">Opaque id, <c>ppv_…</c>.</param>
/// <param name="Status"><c>draft</c> or <c>published</c>.</param>
/// <param name="IsCurrent">True for the version the public page shows and consents are stamped with.</param>
/// <param name="Label">Set on publishing.</param>
/// <param name="EffectiveDate">Set on publishing.</param>
/// <param name="PublishedAt">Set on publishing.</param>
/// <param name="PublishedByStaffId">Who published it.</param>
/// <param name="LegalReviewDone">The owner's statement, made on publishing, that a lawyer reviewed it.</param>
/// <param name="CreatedFrom"><c>template</c> or <c>version</c>.</param>
/// <param name="TemplateVersion">The shipped template the text descends from, if any.</param>
/// <param name="BasedOnVersionId">The version it was copied from, if any.</param>
/// <param name="CreatedByStaffId">Who made the draft; absent for the one the migration seeded.</param>
/// <param name="CreatedAt">When the draft was made.</param>
/// <param name="UpdatedAt">The last change.</param>
/// <param name="Version">Echo as <c>If-Match</c>.</param>
/// <param name="Sections">The text.</param>
public sealed record StaffPrivacyPolicyVersionDto(
    string Id,
    string Status,
    bool IsCurrent,
    string? Label,
    DateOnly? EffectiveDate,
    DateTimeOffset? PublishedAt,
    string? PublishedByStaffId,
    bool LegalReviewDone,
    string CreatedFrom,
    string? TemplateVersion,
    string? BasedOnVersionId,
    string? CreatedByStaffId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    uint Version,
    IReadOnlyList<PrivacyPolicySectionDto> Sections)
{
    public static StaffPrivacyPolicyVersionDto From(PrivacyPolicyVersion version, string? currentId) => new(
        version.Id,
        version.IsPublished ? "published" : "draft",
        version.Id == currentId,
        version.Label,
        version.EffectiveDate,
        version.PublishedAt,
        version.PublishedByStaffId,
        version.LegalReviewDone,
        WireEnum.Of(version.CreatedFrom),
        version.TemplateVersion,
        version.BasedOnVersionId,
        version.CreatedByStaffId,
        version.CreatedAt,
        version.UpdatedAt,
        version.Version,
        [.. version.Sections.Select(PrivacyPolicySectionDto.From)]);
}

/// <summary>The template that ships with this release, and whether the clinic has made a draft from it.</summary>
/// <param name="Version">The template's own version, <c>2026-10-template-1</c>.</param>
/// <param name="NewerAvailable">
/// True when no version descends from this template: a release has shipped a newer template than
/// the one the clinic's text came from. Nothing changes until staff make a draft from it.
/// </param>
public sealed record PrivacyPolicyTemplateDto(string Version, bool NewerAvailable);

/// <summary><c>GET /staff/privacy-policy/versions</c>.</summary>
/// <param name="Data">Every version, newest made first.</param>
/// <param name="CurrentVersionId">The current notice, or absent when nothing is published.</param>
/// <param name="Template">The shipped template.</param>
public sealed record StaffPrivacyPolicyListDto(
    IReadOnlyList<StaffPrivacyPolicyVersionDto> Data,
    string? CurrentVersionId,
    PrivacyPolicyTemplateDto Template);
