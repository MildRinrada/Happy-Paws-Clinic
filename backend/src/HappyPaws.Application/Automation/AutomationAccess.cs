using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation;

/// <summary>
/// Loads a rule for a member of staff, with the branch rule every staff read has.
/// <para>
/// A rule of another branch is <c>403</c>, as for any staff record of another branch
/// (<c>docs/authorization.md</c>). A clinic-wide rule is visible to everyone who may read rules,
/// and whatever it shows a branch-bound reader is narrowed to that reader's branches by
/// <see cref="Visible"/>.
/// </para>
/// </summary>
public sealed class AutomationAccess(IAutomationRuleRepository rules, BranchGuard branchGuard)
{
    public async Task<AutomationRule> LoadAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await rules.FindAsync(ruleId, cancellationToken) ?? throw new NotFoundException("automation", ruleId);

        if (rule.BranchId is { } branchId)
        {
            branchGuard.EnsureCanAccess(branchId);
        }

        return rule;
    }

    /// <summary>
    /// Loads a rule, tracked, for an edit or a delete (<c>docs/contract-extensions.md § 19</c>).
    /// <para>
    /// Stricter than reading, the same line create draws: a clinic-wide rule reaches every branch's
    /// customers, so only a caller the branch guard does not restrict may change one. A branch-bound
    /// reader still sees it, and still previews it.
    /// </para>
    /// </summary>
    public async Task<AutomationRule> LoadForChangeAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await rules.FindTrackedAsync(ruleId, cancellationToken) ?? throw new NotFoundException("automation", ruleId);

        if (rule.BranchId is { } branchId)
        {
            branchGuard.EnsureCanAccess(branchId);
        }
        else if (branchGuard.VisibleBranchIds is not null)
        {
            throw new ForbiddenException("a branch-bound caller cannot change a clinic-wide rule");
        }

        return rule;
    }

    /// <summary>
    /// Whether a reminder's subject may be shown to this reader: its branch is one of theirs, or it
    /// belongs to no branch. The rule still sends it; this only decides who sees it in a preview.
    /// </summary>
    public bool Visible(AutomationOccurrence occurrence) =>
        occurrence.Subject.BranchId is not { } branchId || branchGuard.CanAccess(branchId);
}
