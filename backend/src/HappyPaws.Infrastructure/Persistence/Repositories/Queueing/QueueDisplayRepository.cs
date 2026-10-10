using HappyPaws.Application.Queueing.Displays;
using HappyPaws.Domain.Queueing;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Queueing;

internal sealed class QueueDisplayRepository(HappyPawsDbContext db) : IQueueDisplayRepository
{
    public void Add(QueueDisplay display) => db.Set<QueueDisplay>().Add(display);

    public Task<QueueDisplay?> FindAsync(string displayId, CancellationToken cancellationToken = default) =>
        db.Set<QueueDisplay>().AsNoTracking().FirstOrDefaultAsync(display => display.Id == displayId, cancellationToken);

    public Task<QueueDisplay?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.Set<QueueDisplay>().AsNoTracking().FirstOrDefaultAsync(display => display.TokenHash == tokenHash, cancellationToken);

    public Task<QueueDisplay?> FindBySetupCodeHashAsync(string codeHash, CancellationToken cancellationToken = default) =>
        db.Set<QueueDisplay>().AsNoTracking().FirstOrDefaultAsync(display => display.SetupCodeHash == codeHash, cancellationToken);

    public async Task<IReadOnlyList<QueueDisplay>> ListAsync(
        IReadOnlyCollection<string>? branchIds,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<QueueDisplay>().AsNoTracking();

        if (branchIds is not null)
        {
            var visible = branchIds.ToArray();
            query = query.Where(display => visible.Contains(display.BranchId));
        }

        return await query
            .OrderByDescending(display => display.CreatedAt).ThenByDescending(display => display.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ClaimAsync(
        string displayId,
        string codeHash,
        string tokenHash,
        DateTimeOffset at,
        CancellationToken cancellationToken = default) =>
        await db.Set<QueueDisplay>()
            .Where(display => display.Id == displayId
                && display.SetupCodeHash == codeHash
                && display.TokenHash == null
                && display.RevokedAt == null)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(display => display.TokenHash, tokenHash)
                    .SetProperty(display => display.SetupCodeHash, (string?)null)
                    .SetProperty(display => display.SetupExpiresAt, (DateTimeOffset?)null)
                    .SetProperty(display => display.ClaimedAt, at)
                    .SetProperty(display => display.LastSeenAt, at)
                    .SetProperty(display => display.UpdatedAt, at),
                cancellationToken) > 0;

    public async Task<bool> RevokeAsync(string displayId, DateTimeOffset at, CancellationToken cancellationToken = default) =>
        await db.Set<QueueDisplay>()
            .Where(display => display.Id == displayId && display.RevokedAt == null)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(display => display.RevokedAt, at)
                    .SetProperty(display => display.SetupCodeHash, (string?)null)
                    .SetProperty(display => display.UpdatedAt, at),
                cancellationToken) > 0;

    public Task TouchAsync(string displayId, DateTimeOffset at, CancellationToken cancellationToken = default) =>
        db.Set<QueueDisplay>()
            .Where(display => display.Id == displayId)
            .ExecuteUpdateAsync(set => set.SetProperty(display => display.LastSeenAt, at), cancellationToken);

    public Task<bool> IsUsableAsync(string displayId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        db.Set<QueueDisplay>().AsNoTracking().AnyAsync(
            display => display.Id == displayId
                && display.TokenHash != null
                && display.RevokedAt == null
                && (display.ExpiresAt == null || display.ExpiresAt > now),
            cancellationToken);
}
