using HappyPaws.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HappyPaws.Infrastructure.Persistence;

/// <summary>
/// Transaction boundary over the EF Core context.
/// <para>
/// Nesting is handled rather than forbidden: if a transaction is already open, the inner call
/// joins it instead of starting a second one, so a use case can be composed into a larger one
/// without either having to know about the other.
/// </para>
/// <para>
/// External calls — LINE push, payment capture, webhook delivery — must not happen inside the
/// callback. A slow gateway would hold row locks for its whole timeout, and a failed push would
/// roll back a signed medical record. Enqueue them and let the dispatcher send after commit.
/// </para>
/// </summary>
public sealed class UnitOfWork(HappyPawsDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            await operation(cancellationToken);
            return;
        }

        // The execution strategy owns the retry loop, so a transient connection drop retries the
        // whole transaction rather than half-applying it.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Every attempt starts from a clean change tracker.
            //
            // The strategy re-runs this whole callback on a transient failure, but entities the
            // failed attempt added are still tracked as Added. Without the reset, a retry writes
            // the previous attempt's work a second time — and that is not hypothetical: two
            // concurrent bookings of one slot deadlock, both retry, and each then tries to insert
            // two overlapping appointments, so the slot exclusion constraint refuses both and
            // *nobody* gets the appointment.
            db.ChangeTracker.Clear();

            await using IDbContextTransaction transaction =
                await db.Database.BeginTransactionAsync(cancellationToken);

            await operation(cancellationToken);
            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    /// <summary>
    /// Saves, converting the constraint violations that back a contract rule into that rule's own
    /// error. See <see cref="PostgresErrorTranslator"/> for why this belongs here and not in a
    /// use case.
    /// </summary>
    private async Task<int> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresErrorTranslator.Translate(exception) is { } translated)
        {
            throw translated;
        }
    }
}
