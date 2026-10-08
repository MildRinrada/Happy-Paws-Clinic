using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation;

/// <summary>
/// The reminder rules. A deleted rule (<c>docs/contract-extensions.md § 19</c>) is kept for its
/// history but found by none of these: every read here is of live rules only.
/// </summary>
public interface IAutomationRuleRepository
{
    void Add(AutomationRule rule);

    Task<AutomationRule?> FindAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>The same lookup, tracked, for the handlers that change a rule.</summary>
    Task<AutomationRule?> FindTrackedAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every rule a reader may see, newest first: the clinic-wide ones and those of
    /// <paramref name="visibleBranchIds"/>, or all of them when that is null.
    /// </summary>
    Task<IReadOnlyList<AutomationRule>> ListAsync(
        IReadOnlyCollection<string>? visibleBranchIds,
        CancellationToken cancellationToken = default);

    /// <summary>The switched-on rules, oldest first: what the scheduler runs.</summary>
    Task<IReadOnlyList<AutomationRule>> EnabledAsync(CancellationToken cancellationToken = default);
}
