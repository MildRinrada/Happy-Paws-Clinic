using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation;

/// <summary>
/// The "already sent" records, and the claim that makes a reminder go out once.
/// <para>
/// Writes go through a context of their own and commit at once, like the delivery log: a claim
/// must be durable before the message leaves, and must not be what flushes anything else a
/// request's context happens to hold.
/// </para>
/// </summary>
public interface IAutomationDispatchRepository
{
    /// <summary>
    /// Inserts the claim unless one for the same rule, subject and due key exists. True when this
    /// caller now owns the reminder and must send it; false when another worker, or an earlier
    /// run, already has it. Decided by the database's unique index, so it holds across instances.
    /// </summary>
    Task<bool> TryClaimAsync(AutomationDispatch claim, CancellationToken cancellationToken = default);

    /// <summary>Records how a claimed reminder ended.</summary>
    Task CompleteAsync(
        string dispatchId,
        DispatchStatus status,
        string? errorCode,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    /// <summary>The <c>subject_id|due_key</c> pairs this rule has already claimed among these subjects.</summary>
    Task<IReadOnlySet<string>> ClaimedKeysAsync(
        string ruleId,
        IReadOnlyCollection<string> subjectIds,
        CancellationToken cancellationToken = default);

    /// <summary>Claims still open that were taken before <paramref name="claimedBefore"/>.</summary>
    Task<IReadOnlyList<AutomationDispatch>> StaleClaimsAsync(
        DateTimeOffset claimedBefore,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>How often each rule has fired, overall and since <paramref name="recentSince"/>.</summary>
    Task<IReadOnlyDictionary<string, AutomationStats>> StatsAsync(
        IReadOnlyCollection<string> ruleIds,
        DateTimeOffset recentSince,
        CancellationToken cancellationToken = default);
}

/// <summary>The key a claimed pair is reported under by <see cref="IAutomationDispatchRepository.ClaimedKeysAsync"/>.</summary>
public static class DispatchKey
{
    public static string Of(string subjectId, string dueKey) => $"{subjectId}|{dueKey}";
}

/// <summary>One rule's history: "สถิติการยิงย้อนหลัง".</summary>
/// <param name="Total">Every reminder it has claimed.</param>
/// <param name="Sent">Accepted on a channel.</param>
/// <param name="Skipped">Not attempted: no consent, no address.</param>
/// <param name="Failed">Refused on every channel tried.</param>
/// <param name="Lost">Claimed by a worker that stopped before sending.</param>
/// <param name="Recent">Claimed since the recent cut-off (30 days).</param>
/// <param name="LastFiredAt">The latest claim.</param>
public sealed record AutomationStats(
    int Total,
    int Sent,
    int Skipped,
    int Failed,
    int Lost,
    int Recent,
    DateTimeOffset? LastFiredAt)
{
    public static readonly AutomationStats None = new(0, 0, 0, 0, 0, 0, null);
}
