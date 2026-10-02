namespace HappyPaws.Application.Queueing;

/// <summary>
/// Allocates the next queue number for a branch on a day — "คิวรีเซ็ตรายวันต่อสาขา".
/// <para>
/// Its own port because "next number" is a concurrency problem, not a domain rule. Two people
/// checking in at the same second must not both be told they are A014, and the only place that can
/// be settled is the database. The implementation serialises allocation per branch per day; see
/// <c>Infrastructure/Persistence/Repositories/Queueing/</c>.
/// </para>
/// <para>
/// Must be called inside the caller's transaction, so a check-in that rolls back does not burn a
/// number and leave a gap in the day's sequence.
/// </para>
/// </summary>
public interface IQueueNumberGenerator
{
    /// <param name="branchId">The branch whose sequence is being advanced.</param>
    /// <param name="serviceDate">The clinic-local day the ticket belongs to.</param>
    /// <param name="resetDaily">
    /// The clinic's "การรีเซ็ตรายวัน" setting. True restarts at 1 each day; false carries the
    /// branch's sequence on from its highest number ever issued.
    /// </param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    Task<int> NextAsync(
        string branchId,
        DateOnly serviceDate,
        bool resetDaily,
        CancellationToken cancellationToken = default);
}
