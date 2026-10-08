using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Automation.SimulateAutomation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation.ListUpcomingReminders;

/// <summary><c>GET /staff/reminders/upcoming?days=&amp;branch=&amp;automation_id=</c>.</summary>
/// <param name="Days">How far ahead, 1 to 30; 7 when absent ("ใน 7 วันข้างหน้า").</param>
/// <param name="BranchId">Only reminders of this branch.</param>
/// <param name="AutomationId">Only this rule's reminders.</param>
public sealed record ListUpcomingRemindersQuery(int? Days = null, string? BranchId = null, string? AutomationId = null);

/// <summary>
/// "คิวข้อความที่จะถูกส่งใน 7 วันข้างหน้า" — every reminder the switched-on rules will send, in
/// order.
/// <para>
/// Not a stored queue. Each enabled rule is evaluated by <see cref="AutomationEvaluator"/> over the
/// window, exactly as a simulation is, and what was already sent is left out. So the list is
/// always what the scheduler will do given today's records: an appointment cancelled a minute ago
/// is already gone from it, and a vaccine recorded a minute ago already moved its reminder.
/// </para>
/// </summary>
public sealed class ListUpcomingRemindersHandler(
    IAutomationRuleRepository rules,
    AutomationAccess access,
    AutomationEvaluator evaluator,
    BranchGuard branchGuard,
    IClock clock)
{
    public async Task<UpcomingRemindersDto> HandleAsync(
        ListUpcomingRemindersQuery query,
        CancellationToken cancellationToken = default)
    {
        var days = SimulateAutomationHandler.ResolveDays(query.Days);
        var branchId = query.BranchId is { Length: > 0 } given ? given.Trim() : null;

        if (branchId is not null)
        {
            branchGuard.EnsureCanAccess(branchId);
        }

        IReadOnlyList<AutomationRule> selected = query.AutomationId is { Length: > 0 } automationId
            ? [await access.LoadAsync(automationId.Trim(), cancellationToken)]
            : await rules.ListAsync(branchGuard.VisibleBranchIds, cancellationToken);

        var now = clock.UtcNow;
        var horizon = now.AddDays(days);
        var reminders = new List<AutomationOccurrence>();

        foreach (var rule in selected.Where(rule => rule.Enabled))
        {
            reminders.AddRange((await evaluator.EvaluateAsync(rule, now, horizon, cancellationToken))
                .Where(occurrence => occurrence.Status != OccurrenceStatus.AlreadySent)
                .Where(access.Visible)
                .Where(occurrence => branchId is null || occurrence.Subject.BranchId == branchId));
        }

        var ordered = reminders
            .OrderBy(occurrence => occurrence.SendAt)
            .ThenBy(occurrence => occurrence.Subject.CustomerName, StringComparer.Ordinal)
            .ToList();

        return new UpcomingRemindersDto(
            now - evaluator.CatchUp,
            horizon,
            ordered.Count,
            ordered.Count > SimulateAutomationHandler.MaxRows,
            [.. ordered.Take(SimulateAutomationHandler.MaxRows).Select(ReminderDto.From)]);
    }
}
