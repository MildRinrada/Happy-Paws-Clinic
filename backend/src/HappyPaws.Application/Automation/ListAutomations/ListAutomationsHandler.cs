using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;

namespace HappyPaws.Application.Automation.ListAutomations;

/// <summary>
/// <c>GET /staff/automations</c> — "กติกาทั้งหมดพร้อมสถิติการยิงย้อนหลัง". Every rule the reader may
/// see, newest first, each with how often it has fired: overall, by outcome, and in the last 30
/// days. A branch-bound reader sees their branches' rules and the clinic-wide ones.
/// </summary>
public sealed class ListAutomationsHandler(
    IAutomationRuleRepository rules,
    IAutomationDispatchRepository dispatches,
    BranchGuard branchGuard,
    IClock clock)
{
    public const int RecentDays = 30;

    public async Task<IReadOnlyList<AutomationDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var list = await rules.ListAsync(branchGuard.VisibleBranchIds, cancellationToken);

        if (list.Count == 0)
        {
            return [];
        }

        var stats = await dispatches.StatsAsync(
            list.Select(rule => rule.Id).ToList(),
            clock.UtcNow.AddDays(-RecentDays),
            cancellationToken);

        return [.. list.Select(rule => AutomationDto.From(rule, stats.GetValueOrDefault(rule.Id, AutomationStats.None)))];
    }
}
