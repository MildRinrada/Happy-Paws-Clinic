using System.Globalization;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Scheduling;

/// <summary>
/// The part of RFC 5545 RRULE that a staff roster needs — "สร้างเวรเดี่ยวหรือทั้งสัปดาห์ด้วย RRULE".
/// <para>
/// Supported: <c>FREQ=DAILY|WEEKLY</c>, <c>INTERVAL</c>, <c>BYDAY=MO,TU,…</c> (weekly only),
/// and one end, <c>COUNT</c> or <c>UNTIL</c> (<c>YYYYMMDD</c>, or a UTC <c>YYYYMMDDTHHMMSSZ</c>).
/// Anything else (monthly rules, <c>BYSETPOS</c>, exceptions) is refused rather than half-read:
/// a roster that silently ignores part of its rule is worse than one that says it cannot.
/// </para>
/// <para>
/// Expansion happens on the clinic's wall clock. "09:00 every Monday" stays 09:00 local on every
/// Monday, whatever the UTC offset does in between.
/// </para>
/// </summary>
public static class ShiftRecurrence
{
    /// <summary>A rule may not produce more shifts than this in one request.</summary>
    public const int MaxOccurrences = 200;

    /// <summary>Nor reach further than this past the first shift.</summary>
    public static readonly TimeSpan MaxHorizon = TimeSpan.FromDays(366);

    private static readonly Dictionary<string, DayOfWeek> Days = new(StringComparer.Ordinal)
    {
        ["MO"] = DayOfWeek.Monday,
        ["TU"] = DayOfWeek.Tuesday,
        ["WE"] = DayOfWeek.Wednesday,
        ["TH"] = DayOfWeek.Thursday,
        ["FR"] = DayOfWeek.Friday,
        ["SA"] = DayOfWeek.Saturday,
        ["SU"] = DayOfWeek.Sunday,
    };

    /// <summary>
    /// The start of every occurrence, as clinic-local dates and times converted back to instants.
    /// Without a rule, just the first one.
    /// </summary>
    /// <param name="firstStart">The first shift's start, which is also the rule's DTSTART.</param>
    /// <param name="rule">The RRULE text, with or without the <c>RRULE:</c> prefix; null for a single shift.</param>
    /// <param name="clinicTimeZone">The clinic's zone, where wall-clock times are kept.</param>
    public static IReadOnlyList<DateTimeOffset> Expand(DateTimeOffset firstStart, string? rule, TimeZoneInfo clinicTimeZone)
    {
        if (string.IsNullOrWhiteSpace(rule))
        {
            return [firstStart];
        }

        var parts = Parse(rule);
        var local = TimeZoneInfo.ConvertTime(firstStart, clinicTimeZone);
        var startDate = DateOnly.FromDateTime(local.DateTime);
        var timeOfDay = TimeOnly.FromDateTime(local.DateTime);

        var frequency = parts.GetValueOrDefault("FREQ") switch
        {
            "DAILY" => 1,
            "WEEKLY" => 7,
            null => throw Invalid("RRULE ต้องมี FREQ"),
            _ => throw Invalid("รองรับเฉพาะ FREQ=DAILY หรือ FREQ=WEEKLY"),
        };

        var interval = parts.TryGetValue("INTERVAL", out var intervalText) ? PositiveInt(intervalText, "INTERVAL") : 1;
        int? count = parts.TryGetValue("COUNT", out var countText) ? PositiveInt(countText, "COUNT") : null;
        DateOnly? until = parts.TryGetValue("UNTIL", out var untilText) ? UntilDate(untilText, clinicTimeZone) : null;

        if (count is null && until is null)
        {
            throw Invalid("RRULE ต้องมี COUNT หรือ UNTIL เพื่อบอกว่าจบเมื่อไร");
        }

        if (count is not null && until is not null)
        {
            throw Invalid("ใส่ COUNT หรือ UNTIL อย่างใดอย่างหนึ่ง");
        }

        var byDay = parts.TryGetValue("BYDAY", out var byDayText) ? DaysOf(byDayText) : null;

        if (byDay is not null && frequency != 7)
        {
            throw Invalid("BYDAY ใช้ได้กับ FREQ=WEEKLY เท่านั้น");
        }

        var lastDate = DateOnly.FromDateTime(local.DateTime.Add(MaxHorizon));
        var dates = new List<DateOnly>();

        if (frequency == 1)
        {
            for (var date = startDate; Accept(date); date = date.AddDays(interval))
            {
            }
        }
        else
        {
            var days = byDay ?? [local.DayOfWeek];
            var weekStart = startDate.AddDays(-(((int)startDate.DayOfWeek + 6) % 7));

            var running = true;

            for (var week = weekStart; running; week = week.AddDays(7 * interval))
            {
                foreach (var date in Enumerable.Range(0, 7).Select(week.AddDays).Where(date => days.Contains(date.DayOfWeek)))
                {
                    if (date >= startDate && !Accept(date))
                    {
                        running = false;
                        break;
                    }
                }
            }
        }

        return [.. dates.Select(date => ToInstant(date, timeOfDay, clinicTimeZone))];

        // Adds the date if the rule still runs; false once it has ended.
        bool Accept(DateOnly date)
        {
            if (until is { } end && date > end)
            {
                return false;
            }

            if (count is { } limit && dates.Count >= limit)
            {
                return false;
            }

            if (date > lastDate)
            {
                throw Invalid($"เวรที่สร้างซ้ำต้องอยู่ภายใน {MaxHorizon.Days} วันนับจากเวรแรก");
            }

            if (dates.Count >= MaxOccurrences)
            {
                throw Invalid($"สร้างเวรได้ครั้งละไม่เกิน {MaxOccurrences} เวร");
            }

            dates.Add(date);
            return true;
        }
    }

    private static Dictionary<string, string> Parse(string rule)
    {
        var text = rule.Trim();

        if (text.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
        {
            text = text[6..];
        }

        var parts = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);

            if (pair.Length != 2 || pair[1].Length == 0)
            {
                throw Invalid($"อ่าน RRULE ส่วน \"{part}\" ไม่ได้");
            }

            var key = pair[0].Trim().ToUpperInvariant();

            if (key is not ("FREQ" or "INTERVAL" or "COUNT" or "UNTIL" or "BYDAY" or "WKST"))
            {
                throw Invalid($"ยังไม่รองรับ {key} ใน RRULE");
            }

            if (!parts.TryAdd(key, pair[1].Trim().ToUpperInvariant()))
            {
                throw Invalid($"RRULE มี {key} ซ้ำ");
            }
        }

        return parts;
    }

    private static HashSet<DayOfWeek> DaysOf(string text)
    {
        var days = new HashSet<DayOfWeek>();

        foreach (var code in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Days.TryGetValue(code, out var day))
            {
                throw Invalid($"BYDAY \"{code}\" ไม่ถูกต้อง ใช้ MO TU WE TH FR SA SU");
            }

            days.Add(day);
        }

        return days.Count > 0 ? days : throw Invalid("BYDAY ต้องมีอย่างน้อยหนึ่งวัน");
    }

    private static DateOnly UntilDate(string text, TimeZoneInfo clinicTimeZone)
    {
        if (DateOnly.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        if (DateTime.TryParseExact(
                text,
                "yyyyMMdd'T'HHmmss'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var instant))
        {
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instant, clinicTimeZone));
        }

        throw Invalid("UNTIL ต้องเป็น YYYYMMDD หรือ YYYYMMDDTHHMMSSZ");
    }

    private static int PositiveInt(string text, string name) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is > 0 and <= 1000
            ? value
            : throw Invalid($"{name} ต้องเป็นจำนวนเต็มบวก");

    private static DateTimeOffset ToInstant(DateOnly date, TimeOnly time, TimeZoneInfo clinicTimeZone)
    {
        var wallClock = date.ToDateTime(time);

        return new DateTimeOffset(wallClock, clinicTimeZone.GetUtcOffset(wallClock));
    }

    private static DomainException Invalid(string message) => new("invalid_recurrence", message);
}
