using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations;

/// <summary>
/// The data a chat button carries: <c>action=confirm_appointment&amp;appointment_id=apt_…</c>.
/// <para>
/// A query string because that is what LINE's own examples use and it survives being read by a
/// person in a log. It is <b>not trusted</b>: whoever can reach the webhook with a valid signature
/// is LINE, but the ids inside were written by us into a message and come back as the customer's
/// claim. Every action checks them against the customer again before doing anything.
/// </para>
/// </summary>
public sealed record LinePostbackData(string Action, IReadOnlyDictionary<string, string> Values)
{
    /// <summary>LINE's limit on postback data.</summary>
    public const int MaxLength = 300;

    public const string AppointmentId = "appointment_id";
    public const string TicketId = "ticket_id";
    public const string InvoiceId = "invoice_id";

    /// <summary>Set on the second tap of a cancellation, after the customer was asked to make sure.</summary>
    public const string Confirmed = "confirm";

    public string? this[string key] => Values.GetValueOrDefault(key);

    public static string Build(string action, params (string Key, string Value)[] values) =>
        string.Join(
            '&',
            [$"action={Uri.EscapeDataString(action)}", .. values.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}")]);

    /// <summary>Null when the data names no action this system knows.</summary>
    public static LinePostbackData? Parse(string? data)
    {
        if (string.IsNullOrWhiteSpace(data) || data.Length > MaxLength)
        {
            return null;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var part in data.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);

            if (separator > 0)
            {
                values[Uri.UnescapeDataString(part[..separator])] = Uri.UnescapeDataString(part[(separator + 1)..]);
            }
        }

        return values.Remove("action", out var action) && LinePostbackActions.All.Contains(action)
            ? new LinePostbackData(action, values)
            : null;
    }
}
