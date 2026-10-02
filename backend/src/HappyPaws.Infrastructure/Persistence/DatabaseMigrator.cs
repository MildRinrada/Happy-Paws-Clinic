using HappyPaws.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence;

/// <summary>EF Core implementation of the explicit migration step.</summary>
internal sealed class DatabaseMigrator(HappyPawsDbContext db) : IDatabaseMigrator
{
    public async Task<IReadOnlyList<string>> ApplyPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count > 0)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        return pending;
    }
}
