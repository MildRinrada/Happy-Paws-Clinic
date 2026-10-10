using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.Application.PrivacyPolicies;

/// <summary>A published version as the history list shows it.</summary>
/// <param name="Label">The version's label.</param>
/// <param name="EffectiveDate">The date printed as "in effect from".</param>
/// <param name="PublishedAt">When it was published.</param>
public sealed record PublishedPolicySummary(string Label, DateOnly EffectiveDate, DateTimeOffset PublishedAt);

/// <summary>Persistence port for the clinic's privacy notice (<c>docs/contract-extensions.md § 35</c>).</summary>
public interface IPrivacyPolicyRepository
{
    void Add(PrivacyPolicyVersion version);

    /// <summary>One version, drafts included, tracked for change. Null when absent.</summary>
    Task<PrivacyPolicyVersion?> FindAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>The current notice: the most recently published version. Null when nothing is published.</summary>
    Task<PrivacyPolicyVersion?> CurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Just the current notice's label, for stamping a consent. Null when nothing is published.</summary>
    Task<string?> CurrentLabelAsync(CancellationToken cancellationToken = default);

    /// <summary>A published version by its label. Drafts have no label and are never found.</summary>
    Task<PrivacyPolicyVersion?> FindPublishedAsync(string label, CancellationToken cancellationToken = default);

    /// <summary>Every published version, newest published first.</summary>
    Task<IReadOnlyList<PublishedPolicySummary>> ListPublishedAsync(CancellationToken cancellationToken = default);

    /// <summary>Every version, drafts included, newest created first.</summary>
    Task<IReadOnlyList<PrivacyPolicyVersion>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>True when a published version already has this label.</summary>
    Task<bool> LabelTakenAsync(string label, CancellationToken cancellationToken = default);
}
