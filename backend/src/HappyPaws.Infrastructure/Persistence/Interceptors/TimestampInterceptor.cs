using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HappyPaws.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <c>CreatedAt</c> and <c>UpdatedAt</c> on every aggregate as it is saved.
/// <para>
/// Cross-cutting on purpose. A timestamp that each use case has to remember to set is a timestamp
/// that will be missing on the one path nobody tested, and these columns are what the audit log
/// and every cursor-paginated list sort on.
/// </para>
/// <para>
/// Values are written through EF's property accessors rather than the CLR setters, so entities
/// keep their setters closed and no public mutator exists for code to misuse.
/// </para>
/// </summary>
public sealed class TimestampInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    Set(entry, nameof(Entity.CreatedAt), now);
                    break;

                case EntityState.Modified:
                    Set(entry, nameof(Entity.UpdatedAt), now);
                    break;
            }
        }
    }

    private static void Set(EntityEntry<Entity> entry, string propertyName, DateTimeOffset value) =>
        entry.Property(propertyName).CurrentValue = value;
}
