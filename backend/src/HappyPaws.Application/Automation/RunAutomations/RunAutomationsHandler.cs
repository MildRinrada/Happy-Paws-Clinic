using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Automation;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Automation.RunAutomations;

/// <summary>What one run did.</summary>
/// <param name="Rules">Enabled rules evaluated.</param>
/// <param name="Claimed">Reminders this worker claimed and handed to the dispatcher.</param>
/// <param name="Contended">Reminders another worker claimed first.</param>
/// <param name="Reconciled">Stale claims settled from the delivery log or marked lost.</param>
public sealed record AutomationRunSummary(int Rules, int Claimed, int Contended, int Reconciled)
{
    public static readonly AutomationRunSummary Empty = new(0, 0, 0, 0);

    public AutomationRunSummary Add(AutomationRunSummary other) =>
        new(Rules + other.Rules, Claimed + other.Claimed, Contended + other.Contended, Reconciled + other.Reconciled);
}

/// <summary>
/// The real run of every enabled rule — what the scheduler calls each tick.
/// <para>
/// For each rule it takes <see cref="AutomationEvaluator.EvaluateAsync"/> at <c>now</c> with no
/// look-ahead, the same call a simulation makes with one, and for each reminder not already sent:
/// <list type="number">
/// <item>claims it by inserting its "already sent" row (<see cref="IAutomationDispatchRepository.TryClaimAsync"/>);
/// if another instance's insert won, it moves on;</item>
/// <item>sends it through <see cref="INotificationDispatcher"/>, with the claim's id as the batch id,
/// so the consent gate, the channel order and the delivery log are exactly those of every other
/// message;</item>
/// <item>reads the outcome back from the delivery log by that batch id and records it on the claim.</item>
/// </list>
/// </para>
/// <para>
/// Nothing here holds a transaction or a lock while a message is in flight: the claim commits on
/// its own before the send, which is what keeps a slow gateway from blocking anything and two
/// instances from sending one reminder twice.
/// </para>
/// </summary>
public sealed class RunAutomationsHandler(
    IAutomationRuleRepository rules,
    AutomationEvaluator evaluator,
    IAutomationDispatchRepository dispatches,
    INotificationDispatcher dispatcher,
    IDeliveryLog deliveryLog,
    IClock clock,
    AutomationSettings settings,
    ILogger<RunAutomationsHandler> logger)
{
    private const int ReconcileBatch = 100;

    public async Task<AutomationRunSummary> RunAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var summary = AutomationRunSummary.Empty with { Reconciled = await ReconcileStaleAsync(now, cancellationToken) };

        foreach (var rule in await rules.EnabledAsync(cancellationToken))
        {
            try
            {
                summary = summary.Add(await RunRuleAsync(rule, now, cancellationToken));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // One broken rule must not stop the others' reminders.
                logger.LogError(exception, "Automation {RuleId} could not be run", rule.Id);
            }
        }

        return summary;
    }

    /// <summary>One rule's real run at <paramref name="now"/>. Public so tests can drive one rule at a time.</summary>
    public async Task<AutomationRunSummary> RunRuleAsync(
        AutomationRule rule,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var claimed = 0;
        var contended = 0;

        foreach (var occurrence in await evaluator.EvaluateAsync(rule, now, now, cancellationToken))
        {
            if (occurrence.Status == OccurrenceStatus.AlreadySent)
            {
                continue;
            }

            var claim = AutomationDispatch.Claim(
                rule.Id,
                occurrence.Subject.Kind,
                occurrence.Subject.SubjectId,
                occurrence.DueKey,
                occurrence.Subject.CustomerId,
                occurrence.Subject.PetId,
                occurrence.Subject.BranchId,
                occurrence.SendAt,
                clock.UtcNow,
                settings.WorkerName);

            if (!await dispatches.TryClaimAsync(claim, cancellationToken))
            {
                contended++;
                continue;
            }

            claimed++;
            await SendAsync(claim, occurrence, cancellationToken);
        }

        return new AutomationRunSummary(1, claimed, contended, 0);
    }

    private async Task SendAsync(AutomationDispatch claim, AutomationOccurrence occurrence, CancellationToken cancellationToken)
    {
        DispatchStatus status;
        string? errorCode;

        try
        {
            await dispatcher.SendAsync(occurrence.Request with { BatchId = claim.Id }, cancellationToken);
            (status, errorCode) = AutomationDispatch.OutcomeOf(await deliveryLog.ForBatchAsync(claim.Id, cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Reminder {DispatchId} of automation {RuleId} was not sent", claim.Id, claim.RuleId);
            (status, errorCode) = (DispatchStatus.Failed, "dispatch_error");
        }

        try
        {
            await dispatches.CompleteAsync(claim.Id, status, errorCode, clock.UtcNow, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The message went; only its bookkeeping did not. The next tick settles it from the log.
            logger.LogError(exception, "Outcome of reminder {DispatchId} was not recorded", claim.Id);
        }
    }

    /// <summary>
    /// Settles claims a stopped worker left open: from the delivery log when it got that far, as
    /// lost when it did not. Never re-sends, because a claim without a log row may still have
    /// reached the provider.
    /// </summary>
    private async Task<int> ReconcileStaleAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var stale = await dispatches.StaleClaimsAsync(now - AutomationSettings.StaleClaim, ReconcileBatch, cancellationToken);

        foreach (var claim in stale)
        {
            var deliveries = await deliveryLog.ForBatchAsync(claim.Id, cancellationToken);
            var (status, errorCode) = deliveries.Count == 0
                ? (DispatchStatus.Lost, "worker_stopped")
                : AutomationDispatch.OutcomeOf(deliveries);

            await dispatches.CompleteAsync(claim.Id, status, errorCode, now, cancellationToken);
        }

        return stale.Count;
    }
}
