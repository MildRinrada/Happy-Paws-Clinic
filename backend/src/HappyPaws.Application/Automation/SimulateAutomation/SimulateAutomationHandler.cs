using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Automation.SimulateAutomation;

/// <summary><c>POST /staff/automations/{id}/simulate</c>.</summary>
/// <param name="AutomationId">The rule.</param>
/// <param name="Days">How far ahead to look, 1 to 30; 7 when absent.</param>
public sealed record SimulateAutomationCommand(string AutomationId, int? Days = null);

/// <summary>
/// "ดูว่าจะส่งถึงใครบ้างก่อนเปิดใช้จริง" — who the rule would message, with what, and when.
/// <para>
/// A dry run of <see cref="AutomationEvaluator"/>, the same evaluator the scheduler runs, so the
/// rows marked <c>due_now</c> are exactly the reminders a real run at this instant would hand to
/// the dispatcher, and the rest are what later ticks would send if nothing changes. Nothing is
/// claimed or sent, and the rule may be switched off: previewing before switching on is the point.
/// </para>
/// <para>
/// A branch-bound reader sees only the reminders of their own branches and those tied to no
/// branch; the counts are of what they see.
/// </para>
/// </summary>
public sealed class SimulateAutomationHandler(AutomationAccess access, AutomationEvaluator evaluator, IClock clock)
{
    public const int DefaultDays = 7;
    public const int MaxDays = 30;
    public const int MaxRows = 500;

    public async Task<SimulationDto> HandleAsync(SimulateAutomationCommand command, CancellationToken cancellationToken = default)
    {
        var days = ResolveDays(command.Days);
        var rule = await access.LoadAsync(command.AutomationId, cancellationToken);

        var now = clock.UtcNow;
        var horizon = now.AddDays(days);
        var occurrences = (await evaluator.EvaluateAsync(rule, now, horizon, cancellationToken))
            .Where(access.Visible)
            .ToList();

        return new SimulationDto(
            rule.Id,
            rule.Enabled,
            now,
            now - evaluator.CatchUp,
            horizon,
            occurrences.Count(occurrence => occurrence.DueNow && occurrence.Status != OccurrenceStatus.AlreadySent),
            occurrences.Count(occurrence => occurrence.Status == OccurrenceStatus.WouldSend),
            occurrences.Count(occurrence => occurrence.Status == OccurrenceStatus.WouldSkip),
            occurrences.Count(occurrence => occurrence.Status == OccurrenceStatus.AlreadySent),
            occurrences.Count > MaxRows,
            [.. occurrences.Take(MaxRows).Select(ReminderDto.From)]);
    }

    internal static int ResolveDays(int? days) => days switch
    {
        null => DefaultDays,
        < 1 or > MaxDays => throw new ValidationFailedException(new Dictionary<string, string[]>
        {
            ["days"] = [$"days ต้องอยู่ระหว่าง 1 ถึง {MaxDays}"],
        }),
        var value => value.Value,
    };
}
