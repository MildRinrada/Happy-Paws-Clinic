using HappyPaws.Application.Automation;
using HappyPaws.Domain.Automation;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Automation;

internal sealed class AutomationRuleRepository(HappyPawsDbContext db) : IAutomationRuleRepository
{
    public void Add(AutomationRule rule) => db.Set<AutomationRule>().Add(rule);

    public Task<AutomationRule?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        db.Set<AutomationRule>().AsNoTracking().FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AutomationRule>> ListAsync(
        IReadOnlyCollection<string>? visibleBranchIds,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<AutomationRule>().AsNoTracking();

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
        await db.Set<AutomationRule>().AsNoTracking()
            .Where(rule => rule.Enabled)
            .OrderBy(rule => rule.CreatedAt)
            .ThenBy(rule => rule.Id)
            .ToListAsync(cancellationToken);
}
