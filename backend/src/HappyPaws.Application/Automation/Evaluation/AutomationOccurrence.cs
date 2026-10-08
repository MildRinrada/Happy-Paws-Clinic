using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation;

/// <summary>What a run would do with one reminder.</summary>
public enum OccurrenceStatus
{
    /// <summary>It would be handed to the dispatcher and, as far as consent goes, may be sent.</summary>
    WouldSend,

    /// <summary>It would be handed to the dispatcher, which would record it skipped (<c>no_consent</c>).</summary>
    WouldSkip,

    /// <summary>This rule already claimed it; a run leaves it alone.</summary>
    AlreadySent,
}

/// <summary>
/// One reminder a rule produces: who, about what, when, and with which words.
/// </summary>
/// <param name="Rule">The rule.</param>
/// <param name="Subject">What it is about.</param>
/// <param name="SendAt">When the rule means it to go out.</param>
/// <param name="DueKey">The second half of its "already sent" key.</param>
/// <param name="DueNow">True when <paramref name="SendAt"/> has passed: a real run at the evaluation instant sends it.</param>
/// <param name="Status">What a run does with it.</param>
/// <param name="SkipReason">Why it would be skipped, when it would.</param>
/// <param name="Request">The message, exactly as the dispatcher would receive it (bar the batch id).</param>
public sealed record AutomationOccurrence(
    AutomationRule Rule,
    AutomationSubject Subject,
    DateTimeOffset SendAt,
    string DueKey,
    bool DueNow,
    OccurrenceStatus Status,
    string? SkipReason,
    NotificationRequest Request);
