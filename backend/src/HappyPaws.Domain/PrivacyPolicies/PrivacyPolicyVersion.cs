using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.PrivacyPolicies;

/// <summary>Where a version's text came from when it was created.</summary>
public enum PrivacyPolicySource
{
    /// <summary>The template shipped in the code (<see cref="PrivacyPolicyTemplate"/>).</summary>
    Template,

    /// <summary>A copy of another version, published or draft.</summary>
    Version,
}

/// <summary>
/// One version of the clinic's privacy notice. <b>Not in the API contract</b>: see
/// <c>docs/contract-extensions.md § 35</c>.
/// <para>
/// The clinic is the data controller under the PDPA, so the notice is the clinic's own text, kept
/// here rather than in the web app's code. A <b>draft</b> can be edited by anyone with
/// <c>settings:write</c>. <b>Publishing</b> (<c>settings:danger</c>, the owner) gives it a label and
/// an effective date and fixes it for good: changing the words means a new version. That is what lets
/// the clinic show, years later, the exact text a customer was shown when they agreed to something:
/// every consent recorded while this version is current carries its <see cref="Label"/>.
/// </para>
/// <para>
/// The newest published version is the current one. Older published versions stay readable by
/// label. Nothing is ever deleted.
/// </para>
/// </summary>
public sealed class PrivacyPolicyVersion : Entity
{
    private readonly List<PrivacyPolicySection> _sections = [];

    private PrivacyPolicyVersion()
    {
    }

    /// <summary>The numbered parts, in order.</summary>
    public IReadOnlyList<PrivacyPolicySection> Sections => _sections;

    public PrivacyPolicySource CreatedFrom { get; private set; }

    /// <summary>
    /// The template the text descends from: set when created from the template, and carried over
    /// when a version is copied. Lets the settings page say when a newer template has shipped.
    /// </summary>
    public string? TemplateVersion { get; private set; }

    /// <summary>The version this one was copied from, when it was.</summary>
    public string? BasedOnVersionId { get; private set; }

    /// <summary>Who created the draft. Null for the draft the migration seeded.</summary>
    public string? CreatedByStaffId { get; private set; }

    /// <summary>The label readers and consents see, <c>2026-11</c>. Null until published.</summary>
    public string? Label { get; private set; }

    /// <summary>The date printed as "in effect from". Null until published.</summary>
    public DateOnly? EffectiveDate { get; private set; }

    /// <summary>When it was published, and so became the current notice. Null for a draft.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    public string? PublishedByStaffId { get; private set; }

    /// <summary>
    /// The owner's statement, made when publishing, that a lawyer has reviewed this text. While it
    /// is false the public page says the notice has not been reviewed yet.
    /// </summary>
    public bool LegalReviewDone { get; private set; }

    public bool IsPublished => PublishedAt is not null;

    /// <summary>
    /// True when the draft was made from the shipped template. The template was written by the
    /// system's developer from what the code does, not by a lawyer, and the editor says so.
    /// </summary>
    public bool IsFromTemplate => CreatedFrom == PrivacyPolicySource.Template;

    /// <summary>A draft holding the shipped template, as the migration seeds it and staff can ask for again.</summary>
    public static PrivacyPolicyVersion DraftFromTemplate(string? createdByStaffId, string? id = null) =>
        Draft(
            PrivacyPolicyTemplate.Sections,
            PrivacyPolicySource.Template,
            PrivacyPolicyTemplate.Version,
            basedOnVersionId: null,
            createdByStaffId,
            id);

    /// <summary>A draft holding a copy of another version's text.</summary>
    public static PrivacyPolicyVersion DraftFrom(PrivacyPolicyVersion source, string createdByStaffId) =>
        Draft(
            source.Sections,
            PrivacyPolicySource.Version,
            source.TemplateVersion,
            source.Id,
            createdByStaffId,
            id: null);

    /// <summary>Replaces every section of a draft. A published version is fixed: <c>409 state_conflict</c>.</summary>
    public void ReplaceSections(IEnumerable<PrivacyPolicySection> sections)
    {
        EnsureDraft("edit");

        var cleaned = PrivacyPolicyRules.Clean(sections);
        EnsureValid(cleaned);

        _sections.Clear();
        _sections.AddRange(cleaned);
    }

    /// <summary>
    /// Makes this draft the current notice from <paramref name="now"/>. The label must be free
    /// (checked by the caller against every published version) and the effective date not before
    /// <paramref name="clinicToday"/>. From here on nothing about the version changes.
    /// </summary>
    public void Publish(
        string label,
        DateOnly effectiveDate,
        bool legalReviewDone,
        string staffId,
        DateTimeOffset now,
        DateOnly clinicToday)
    {
        EnsureDraft("publish");

        label = label.Trim();

        if (!PrivacyPolicyRules.IsLabel(label) || label == PrivacyPolicyRules.LegacyLabel)
        {
            throw new DomainException("invalid_privacy_policy", "เลขฉบับไม่ถูกต้อง");
        }

        if (effectiveDate < clinicToday)
        {
            throw new DomainException("invalid_privacy_policy", "วันที่มีผลต้องไม่ก่อนวันนี้");
        }

        EnsureValid(_sections);

        Label = label;
        EffectiveDate = effectiveDate;
        LegalReviewDone = legalReviewDone;
        PublishedAt = now;
        PublishedByStaffId = staffId;
    }

    private static PrivacyPolicyVersion Draft(
        IReadOnlyList<PrivacyPolicySection> sections,
        PrivacyPolicySource source,
        string? templateVersion,
        string? basedOnVersionId,
        string? createdByStaffId,
        string? id)
    {
        var version = new PrivacyPolicyVersion
        {
            Id = id ?? EntityId.New(EntityId.PrivacyPolicyVersion),
            CreatedFrom = source,
            TemplateVersion = templateVersion,
            BasedOnVersionId = basedOnVersionId,
            CreatedByStaffId = createdByStaffId,
        };

        var cleaned = PrivacyPolicyRules.Clean(sections);
        EnsureValid(cleaned);
        version._sections.AddRange(cleaned);

        return version;
    }

    private void EnsureDraft(string attempted)
    {
        if (IsPublished)
        {
            throw new StateConflictException("privacy_policy_version", "published", attempted);
        }
    }

    private static void EnsureValid(IReadOnlyList<PrivacyPolicySection> sections)
    {
        if (PrivacyPolicyRules.Problems(sections) is { Count: > 0 } problems)
        {
            var (field, messages) = problems.First();
            throw new DomainException("invalid_privacy_policy", $"{field}: {messages[0]}");
        }
    }
}
