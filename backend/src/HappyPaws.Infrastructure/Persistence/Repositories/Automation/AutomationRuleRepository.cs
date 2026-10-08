using HappyPaws.Application.Automation;
using HappyPaws.Domain.Automation;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Automation;

internal sealed class AutomationRuleRepository(HappyPawsDbContext db) : IAutomationRuleRepository
{
    public void Add(AutomationRule rule) => db.Set<AutomationRule>().Add(rule);

    public Task<AutomationRule?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        Live().AsNoTracking().FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);

    public Task<AutomationRule?> FindTrackedAsync(string id, CancellationToken cancellationToken = default) =>
        Live().FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AutomationRule>> ListAsync(
        IReadOnlyCollection<string>? visibleBranchIds,
        CancellationToken cancellationToken = default)
    {
        var query = Live().AsNoTracking();

        if (visibleBranchIds is { } visible)
        {
            var branches = visible.ToList();
            query = query.Where(rule => rule.BranchId == null || branches.Contains(rule.BranchId));
        }

        return await query
            .OrderByDescending(rule => rule.CreatedAt)
            .ThenByDescending(rule => rule.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AutomationRule>> EnabledAsync(CancellationToken cancellationToken = default) =>
        await Live().AsNoTracking()
            .Where(rule => rule.Enabled)
            .OrderBy(rule => rule.CreatedAt)
            .ThenBy(rule => rule.Id)
            .ToListAsync(cancellationToken);

    /// <summary>Rules that have not been deleted. A deleted rule stays only so its history names it.</summary>
    private IQueryable<AutomationRule> Live() => db.Set<AutomationRule>().Where(rule => rule.DeletedAt == null);
}
