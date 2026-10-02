using System.Globalization;

namespace HappyPaws.Application.Clinics;

/// <summary>
/// The wire forms of a clock time and a weekday — <c>"09:00"</c> and <c>"monday"</c>.
/// <para>
/// Both travel as strings on purpose. A JSON <c>TimeOnly</c> must be written <c>09:00:00</c>, which
/// is not how anyone writes opening hours, and an enum weekday would cross the wire as <c>1</c>,
/// which means Monday in .NET and Sunday in half the calendars a client might be built with.
/// </para>
/// </summary>
public static class WireTime
{
    private static readonly string[] TimeFormats = ["HH:mm", "H:mm", "HH:mm:ss"];

    private static readonly string[] DayNames =
    [
        "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday",
    ];

    public static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(
            value?.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    public static string Format(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static bool TryParseDay(string? value, out DayOfWeek day)
    {
        var index = Array.IndexOf(DayNames, value?.Trim().ToLowerInvariant());
        day = index < 0 ? default : (DayOfWeek)index;

        return index >= 0;
    }

    public static string Format(DayOfWeek day) => DayNames[(int)day];
}
