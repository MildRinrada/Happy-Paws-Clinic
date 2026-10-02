using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing;

/// <summary>
/// Turns the wire value into the enum, in one place.
/// <para>
/// Two endpoints accept a priority — issuing a walk-in ticket and changing an existing one — and a
/// second copy of this parse would be a second answer to "what does an unrecognised value mean".
/// Here the answer is a single one: absent means normal, unrecognised is a validation failure.
/// Silently treating a typo'd <c>"emergancy"</c> as normal would put a collapsing patient at the
/// back of the queue.
/// </para>
/// </summary>
public static class QueuePriorityParser
{
    public static QueuePriority Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return QueuePriority.Normal;
        }

        return Enum.TryParse<QueuePriority>(value, ignoreCase: true, out var priority)
            ? priority
            : throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["priority"] = ["ลำดับความเร่งด่วนต้องเป็น normal, urgent หรือ emergency"],
            });
    }

    /// <summary><c>Emergency</c> → <c>emergency</c>, the form the API returns.</summary>
    public static string ToWire(QueuePriority priority) => priority.ToString().ToLowerInvariant();
}
