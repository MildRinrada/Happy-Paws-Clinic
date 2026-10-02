namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// Transaction boundary for multi-step operations — closing a consultation signs the record,
/// issues an invoice, decrements stock and schedules a follow-up, and either all of that lands
/// or none of it does.
/// Implemented over the EF Core <c>DbContext</c> in the Infrastructure layer.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside one transaction, retrying the whole thing on a
    /// transient database failure.
    /// <para>
    /// <b>Load inside the callback, not before it.</b> A retry re-runs the operation from the
    /// start against a reset change tracker, so an entity fetched before this call is detached by
    /// the time the callback mutates it and its changes are silently dropped. Reads that do not
    /// feed a write — an authorization lookup, a catalogue item — are unaffected.
    /// </para>
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
