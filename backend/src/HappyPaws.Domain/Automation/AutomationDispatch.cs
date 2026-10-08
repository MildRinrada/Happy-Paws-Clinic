using HappyPaws.Domain.Common;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Domain.Automation;

/// <summary>What became of one reminder a rule claimed.</summary>
public enum DispatchStatus
{
    /// <summary>Claimed by a worker, not yet handed to the dispatcher (or the worker stopped before it finished).</summary>
    Claimed,

    /// <summary>Handed over and accepted on a channel.</summary>
    Sent,

    /// <summary>Not attempted: no consent, no address, a channel switched off. The code says which.</summary>
    Skipped,

    /// <summary>Every channel that was tried refused it.</summary>
    Failed,

    /// <summary>Claimed, then the worker vanished before anything reached the delivery log. Never sent twice.</summary>
    Lost,
}

/// <summary>
/// The "already sent" record of one reminder: rule, subject, and the due date it was about.
/// <para>
/// <b>The unique key is the guarantee.</b> <c>(rule_id, subject_kind, subject_id, due_key)</c> is
/// unique in the database, and a worker claims a reminder by inserting this row before it sends
/// anything. Two API instances evaluating the same rule at the same moment both try; the database
/// lets exactly one insert succeed, and only that one sends. The same row stops a later tick, a
/// restart or a catch-up after downtime from sending the same reminder again.
/// </para>
/// <para>
/// The due key is the date or start time the reminder was about. A rescheduled appointment has a
/// new start time, so it gets a new reminder; the same appointment at the same time never gets
/// two from one rule.
/// </para>
/// <para>
/// <b>At most once, not at least once.</b> The claim is committed before the message goes out. A
/// worker that dies between the two leaves a <see cref="DispatchStatus.Claimed"/> row and no
/// message; the next tick marks it <see cref="DispatchStatus.Lost"/> rather than risk a duplicate.
/// A missed reminder costs a phone call; two reminders for one visit look like a broken system.
/// </para>
/// </summary>
public sealed class AutomationDispatch : Entity
{
    public const int DueKeyMax = 40;

    private AutomationDispatch()
    {
    }

    public string RuleId { get; private set; } = string.Empty;

    public AutomationSubjectKind SubjectKind { get; private set; }

    /// <summary>The vaccination, appointment, or <c>{plan id}:{session number}</c>.</summary>
    public string SubjectId { get; private set; } = string.Empty;

    /// <summary>The due date or start instant the reminder was about, as text.</summary>
    public string DueKey { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public string? PetId { get; private set; }

    public string? BranchId { get; private set; }

    /// <summary>When the rule meant it to go out.</summary>
    public DateTimeOffset ScheduledFor { get; private set; }

    public DateTimeOffset ClaimedAt { get; private set; }

    /// <summary>Which worker claimed it — host and process — for reading a log after an incident.</summary>
    public string ClaimedBy { get; private set; } = string.Empty;

    public DispatchStatus Status { get; private set; }

    public string? ErrorCode { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static AutomationDispatch Claim(
        string ruleId,
        AutomationSubjectKind subjectKind,
        string subjectId,
        string dueKey,
        string customerId,
        string? petId,
        string? branchId,
        DateTimeOffset scheduledFor,
        DateTimeOffset now,
        string claimedBy)
    {
        if (string.IsNullOrWhiteSpace(subjectId) || string.IsNullOrWhiteSpace(dueKey) || dueKey.Length > DueKeyMax)
        {
            throw new DomainException("invalid_dispatch", "การเตือนต้องมีเรื่องที่เตือนและวันที่ครบกำหนด");
        }

        return new AutomationDispatch
        {
            Id = EntityId.New(EntityId.AutomationDispatch),
            RuleId = ruleId,
            SubjectKind = subjectKind,
            SubjectId = subjectId,
            DueKey = dueKey,
            CustomerId = customerId,
            PetId = petId,
            BranchId = branchId,
            ScheduledFor = scheduledFor,
            ClaimedAt = now,
            ClaimedBy = claimedBy,
            Status = DispatchStatus.Claimed,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// The outcome of a dispatch, read from the delivery log rows it produced.
    /// <para>
    /// Any channel that took it makes it sent. All rows skipped means skipped, with the first reason.
    /// Otherwise it failed. No rows at all means it was handed to a channel that keeps no log (the
    /// stand-in used by tests), which accepted it.
    /// </para>
    /// </summary>
    public static (DispatchStatus Status, string? ErrorCode) OutcomeOf(IReadOnlyList<NotificationDelivery> deliveries)
    {
        if (deliveries.Count == 0 || deliveries.Any(delivery => delivery.Status == DeliveryStatus.Sent))
        {
            return (DispatchStatus.Sent, null);
        }

        if (deliveries.All(delivery => delivery.Status == DeliveryStatus.Skipped))
        {
            return (DispatchStatus.Skipped, deliveries[0].ErrorCode);
        }

        return (DispatchStatus.Failed, deliveries.LastOrDefault(delivery => delivery.Status == DeliveryStatus.Failed)?.ErrorCode);
    }
}
