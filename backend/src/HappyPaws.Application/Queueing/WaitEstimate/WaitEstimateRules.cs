using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Queueing.WaitEstimate;

/// <summary>
/// The arithmetic behind <c>GET /queue/wait-estimate</c> (<c>docs/contract-extensions.md § 37</c>),
/// kept apart from the reads so it can be tested on its own.
/// <para>
/// The estimate is deliberately rough: tickets waiting × the branch's typical time in the room ÷
/// vets on shift now, floored to ten minutes and shown as a ten-minute range. It tells a walk-in
/// "about how long", never a promise, and never anything about who is waiting.
/// </para>
/// </summary>
public static class WaitEstimateRules
{
    /// <summary>Days of served tickets the typical consultation time is read from.</summary>
    public const int HistoryDays = 28;

    /// <summary>Fewer timed consultations than this and the median is not trusted.</summary>
    public const int MinSamples = 5;

    /// <summary>The time in the room used until the branch has <see cref="MinSamples"/> of its own.</summary>
    public const int DefaultConsultationMinutes = 20;

    /// <summary>Estimates are floored to this and shown as a range this wide.</summary>
    public const int BucketMinutes = 10;

    /// <summary>How long one answer is reused, on the server and in the <c>Cache-Control</c> header.</summary>
    public const int CacheSeconds = 30;

    /// <summary>How far ahead a closed branch's next opening is looked for.</summary>
    public const int LookAheadDays = 14;

    /// <summary>
    /// The branch's typical consultation: the median it measured, rounded to a minute, once there
    /// are enough timed visits; <see cref="DefaultConsultationMinutes"/> before that.
    /// </summary>
    public static int ConsultationMinutes(int samples, double? medianMinutes) =>
        samples >= MinSamples && medianMinutes is > 0
            ? Math.Max(1, (int)Math.Round(medianMinutes.Value, MidpointRounding.AwayFromZero))
            : DefaultConsultationMinutes;

    /// <summary>
    /// The range shown: <c>waiting × minutes ÷ vets</c>, floored to <see cref="BucketMinutes"/>, up to
    /// one bucket more. Nobody waiting is <c>0</c> to <c>10</c>. A branch that is open counts at
    /// least one vet, so a roster gap never divides by zero.
    /// </summary>
    public static (int Low, int High) Range(int waiting, int consultationMinutes, int vetsOnDuty)
    {
        if (waiting <= 0)
        {
            return (0, BucketMinutes);
        }

        var minutes = waiting * Math.Max(1, consultationMinutes) / Math.Max(1, vetsOnDuty);
        var low = minutes / BucketMinutes * BucketMinutes;

        return (low, low + BucketMinutes);
    }

    /// <summary>
    /// Whether the branch is open at <paramref name="localNow"/> (clinic time). A holiday that shuts
    /// it today closes it. Otherwise the weekly hours decide, opening time included and closing time
    /// not. A clinic that has never set its hours is open while a vet is on shift there.
    /// </summary>
    public static bool IsOpen(
        OpeningHours hours,
        IReadOnlyCollection<ClinicHoliday> holidays,
        string branchId,
        DateTime localNow,
        int vetsOnDuty)
    {
        var today = DateOnly.FromDateTime(localNow);

        if (ShutBy(holidays, branchId, today))
        {
            return false;
        }

        if (!hours.IsConfigured)
        {
            return vetsOnDuty > 0;
        }

        var time = TimeOnly.FromDateTime(localNow);

        return HoursOn(hours, today) is { } open && time >= open.Opens && time < open.Closes;
    }

    /// <summary>
    /// When a closed branch opens next, in clinic time: later today, or the first open day within
    /// <see cref="LookAheadDays"/> that no holiday shuts. Null when the hours were never set or
    /// nothing opens in that span.
    /// </summary>
    public static DateTime? NextOpening(
        OpeningHours hours,
        IReadOnlyCollection<ClinicHoliday> holidays,
        string branchId,
        DateTime localNow)
    {
        if (!hours.IsConfigured)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(localNow);
        var time = TimeOnly.FromDateTime(localNow);

        for (var offset = 0; offset < LookAheadDays; offset++)
        {
            var day = today.AddDays(offset);

            if (ShutBy(holidays, branchId, day) || HoursOn(hours, day) is not { } open)
            {
                continue;
            }

            if (offset == 0 && time >= open.Opens)
            {
                continue;
            }

            return day.ToDateTime(open.Opens);
        }

        return null;
    }

    private static bool ShutBy(IReadOnlyCollection<ClinicHoliday> holidays, string branchId, DateOnly day) =>
        holidays.Any(holiday => holiday.Date == day && holiday.Closes(branchId));

    private static (TimeOnly Opens, TimeOnly Closes)? HoursOn(OpeningHours hours, DateOnly day) =>
        hours.Days.FirstOrDefault(entry => entry.Day == day.DayOfWeek) is { Opens: { } opens, Closes: { } closes }
            ? (opens, closes)
            : null;
}
