using System.Globalization;
using System.Text;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Integrations.GetCalendarFeed;

/// <summary>
/// Writes an iCalendar (RFC 5545) feed. Hand-written rather than a library: the feed is a dozen
/// properties, and the rules that matter are few: CRLF line ends, text escaping, and folding lines
/// at 75 octets without cutting a Thai character in half.
/// </summary>
public static class IcsCalendar
{
    public const string ContentType = "text/calendar; charset=utf-8";

    public static string Write(string calendarName, string host, IEnumerable<CalendarEntry> entries, DateTimeOffset now)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Happy Paws Clinic//Calendar Feed//TH",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "X-WR-CALNAME:" + Escape(calendarName),
            "X-PUBLISHED-TTL:PT30M",
            "REFRESH-INTERVAL;VALUE=DURATION:PT30M",
        };

        foreach (var entry in entries)
        {
            if (entry.Status == AppointmentStatus.Draft)
            {
                continue;
            }

            lines.Add("BEGIN:VEVENT");
            lines.Add($"UID:{entry.AppointmentId}@{host}");
            lines.Add("DTSTAMP:" + Utc(entry.Stamp == default ? now : entry.Stamp));
            lines.Add("DTSTART:" + Utc(entry.StartsAt));
            lines.Add("DTEND:" + Utc(entry.EndsAt));
            lines.Add("SUMMARY:" + Escape($"{entry.ServiceName} - {entry.PetName}"));
            lines.Add("LOCATION:" + Escape(entry.BranchName));
            lines.Add("STATUS:" + StatusOf(entry.Status));
            lines.Add("DESCRIPTION:" + Escape($"นัด {entry.AppointmentId}"));
            lines.Add("END:VEVENT");
        }

        lines.Add("END:VCALENDAR");

        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            builder.Append(Fold(line)).Append("\r\n");
        }

        return builder.ToString();
    }

    private static string StatusOf(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Cancelled or AppointmentStatus.NoShow => "CANCELLED",
        _ => "CONFIRMED",
    };

    private static string Utc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    /// <summary>RFC 5545 § 3.3.11: backslash, semicolon, comma and newline are escaped.</summary>
    public static string Escape(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\n", StringComparison.Ordinal);

    /// <summary>RFC 5545 § 3.1: lines longer than 75 octets continue on the next line after a space.</summary>
    public static string Fold(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) <= 75)
        {
            return line;
        }

        var builder = new StringBuilder();
        var octets = 0;
        var limit = 75;

        var elements = StringInfo.GetTextElementEnumerator(line);
        while (elements.MoveNext())
        {
            var element = (string)elements.Current;
            var size = Encoding.UTF8.GetByteCount(element);

            if (octets + size > limit)
            {
                builder.Append("\r\n ");
                octets = 0;
                limit = 74;
            }

            builder.Append(element);
            octets += size;
        }

        return builder.ToString();
    }
}
