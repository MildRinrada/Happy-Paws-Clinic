using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation;

/// <summary>
/// Turns a rule into the reminders it produces over a span of time. <b>The one code path</b>:
/// the scheduler's real run, <c>POST /staff/automations/{id}/simulate</c> and
/// <c>GET /staff/reminders/upcoming</c> all call <see cref="EvaluateAsync"/>, and differ only in
/// how far ahead they look and in whether anything is then sent.
/// <para>
/// A run at instant <c>now</c> sends exactly the occurrences of <c>EvaluateAsync(rule, now, now)</c>
/// that are not <see cref="OccurrenceStatus.AlreadySent"/>. A simulation at <c>now</c> calls
/// <c>EvaluateAsync(rule, now, now + days)</c>: its <see cref="AutomationOccurrence.DueNow"/> rows
/// are that same set, because the window, the filters, the subject query, the dedupe lookup and
/// the message are all computed here and nowhere else. That is what "the preview can never differ
/// from the real run" rests on, and what the integration tests assert.
/// </para>
/// <para>
/// Consent is read through the same <see cref="ConsentGate"/> the dispatcher uses. A preview row
/// marked <see cref="OccurrenceStatus.WouldSkip"/> is one the real run still hands to the
/// dispatcher, which records it skipped with <c>no_consent</c>; the gate is never bypassed, and the
/// run never pre-filters it away either, so the delivery log answers "why was the owner not told".
/// </para>
/// </summary>
public sealed class AutomationEvaluator(
    IAutomationSubjectReader subjects,
    IAutomationDispatchRepository dispatches,
    ConsentGate consent,
    IClock clock,
    AutomationSettings settings)
{
    /// <summary>How long after its send time a reminder may still go out.</summary>
    public TimeSpan CatchUp => settings.CatchUp;

    /// <summary>
    /// The reminders <paramref name="rule"/> produces with a send time in
    /// <c>(now - catch-up, horizon]</c>, oldest first.
    /// </summary>
    /// <param name="rule">Enabled or not; a simulation evaluates a rule before it is switched on.</param>
    /// <param name="now">The evaluation instant. Subjects that have already started are left out.</param>
    /// <param name="horizon">The latest send time wanted; <paramref name="now"/> for a real run.</param>
    /// <param name="cancellationToken">Aborts the reads.</param>
    public async Task<IReadOnlyList<AutomationOccurrence>> EvaluateAsync(
        AutomationRule rule,
        DateTimeOffset now,
        DateTimeOffset horizon,
        CancellationToken cancellationToken = default)
    {
        var sendFrom = now - settings.CatchUp;
        var sendTo = horizon < now ? now : horizon;
        var zone = clock.ClinicTimeZone;

        var (subjectFrom, subjectTo) = rule.Timing.SubjectSpan(sendFrom, sendTo);
        var query = new SubjectQuery(
            subjectFrom,
            subjectTo,
            DateOf(subjectFrom, zone),
            DateOf(subjectTo, zone),
            rule.BranchId,
            rule.Species,
            rule.VaccineName,
            rule.ServiceId,
            rule.UnconfirmedOnly);

        var candidates = new List<(AutomationSubject Subject, DateTimeOffset SendAt)>();

        foreach (var subject in await subjects.FindAsync(rule.Trigger, query, cancellationToken))
        {
            var sendAt = rule.Timing.SendAt(subject.StartsAt, subject.Day, zone);

            if (sendAt <= sendFrom || sendAt > sendTo)
            {
                continue;
            }

            // A reminder that fell due while the patient was recorded deceased is stale once the
            // death is undone. Reminders resume from the undo, not from the catch-up window
            // (contract-extensions § 29).
            if (subject.QuietUntil is { } quietUntil && sendAt <= quietUntil)
            {
                continue;
            }

            // A visit that has begun, or one the message would only reach after it began ("the
            // morning of at 08:00" for a 07:30 appointment), is not reminded about.
            if (subject.StartsAt is { } startsAt && (startsAt <= now || startsAt <= sendAt))
            {
                continue;
            }

            candidates.Add((subject, sendAt));
        }

        if (candidates.Count == 0)
        {
            return [];
        }

        var claimed = await dispatches.ClaimedKeysAsync(
            rule.Id,
            candidates.Select(candidate => candidate.Subject.SubjectId).Distinct(StringComparer.Ordinal).ToList(),
            cancellationToken);

        var mayReceive = new Dictionary<string, bool>(StringComparer.Ordinal);
        var occurrences = new List<AutomationOccurrence>(candidates.Count);

        foreach (var (subject, sendAt) in candidates)
        {
            var dueKey = AutomationWire.DueKeyOf(subject);
            OccurrenceStatus status;
            string? reason = null;

            if (claimed.Contains(DispatchKey.Of(subject.SubjectId, dueKey)))
            {
                status = OccurrenceStatus.AlreadySent;
            }
            else
            {
                if (!mayReceive.TryGetValue(subject.CustomerId, out var allowed))
                {
                    allowed = await consent.MayReceiveAsync(subject.CustomerId, rule.TemplateKey, cancellationToken);
                    mayReceive[subject.CustomerId] = allowed;
                }

                status = allowed ? OccurrenceStatus.WouldSend : OccurrenceStatus.WouldSkip;
                reason = allowed ? null : DeliveryErrors.NoConsent;
            }

            occurrences.Add(new AutomationOccurrence(
                rule,
                subject,
                sendAt,
                dueKey,
                DueNow: sendAt <= now,
                status,
                reason,
                AutomationMessages.RequestFor(rule, subject, zone)));
        }

        return [.. occurrences
            .OrderBy(occurrence => occurrence.SendAt)
            .ThenBy(occurrence => occurrence.Subject.CustomerName, StringComparer.Ordinal)
            .ThenBy(occurrence => occurrence.Subject.SubjectId, StringComparer.Ordinal)];
    }

    private static DateOnly DateOf(DateTimeOffset instant, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);
}
