using HappyPaws.Application.Common;
using HappyPaws.Domain.Automation;

namespace HappyPaws.Application.Automation;

/// <summary>The wire names of the automation vocabulary: <c>vaccine_due</c>, <c>line</c>, <c>would_send</c>.</summary>
public static class AutomationWire
{
    /// <summary>The only action there is today: send a message through the dispatcher.</summary>
    public const string SendNotification = "send_notification";

    public static string ToWire(AutomationTrigger trigger) => WireEnum.Of(trigger);

    public static string ToWire(AutomationChannel channel) => WireEnum.Of(channel);

    public static string ToWire(AutomationSubjectKind kind) => WireEnum.Of(kind);

    public static string ToWire(OccurrenceStatus status) => WireEnum.Of(status);

    /// <summary>A due key as stored: the day for a date, the UTC instant to the second for a start.</summary>
    public static string DueKeyOf(AutomationSubject subject) =>
        subject.StartsAt is { } startsAt
            ? startsAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture)
            : subject.Day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
