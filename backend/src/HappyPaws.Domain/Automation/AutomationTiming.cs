namespace HappyPaws.Domain.Automation;

/// <summary>
/// When, relative to its subject, a rule sends. Exactly one of the two shapes:
/// <list type="bullet">
/// <item><see cref="OffsetDays"/> with <see cref="SendTime"/>: on the clinic day that many days
/// before the subject's day, at that clinic time. "14 days before the vaccine is due, at 09:00",
/// "the evening before at 18:00", "the morning of at 08:00" (zero days).</item>
/// <item><see cref="OffsetHours"/>: that many hours before the subject's own instant. "T-24h",
/// "T-2h". Only for subjects that have a time, not a vaccine's due date.</item>
/// </list>
/// <para>
/// A negative <see cref="OffsetDays"/> means after the day: "seven days after the vaccine fell
/// due" is a follow-up for an overdue dose.
/// </para>
/// </summary>
/// <param name="OffsetDays">Days before the subject's clinic day.</param>
/// <param name="OffsetHours">Hours before the subject's instant.</param>
/// <param name="SendTime">Clinic time of day, with <paramref name="OffsetDays"/>.</param>
public sealed record AutomationTiming(int? OffsetDays, int? OffsetHours, TimeOnly? SendTime)
{
    /// <summary>The time of day a day-based rule sends when nobody said: the start of a clinic morning.</summary>
    public static readonly TimeOnly DefaultSendTime = new(9, 0);

    public bool IsHourBased => OffsetHours is not null;

    /// <summary>
    /// The instant this rule sends its message about a subject.
    /// </summary>
    /// <param name="subjectInstant">The appointment's or session's start; null for a vaccine due date.</param>
    /// <param name="subjectDate">The subject's clinic day.</param>
    /// <param name="clinicZone">
    /// The clinic's zone. Passed in because the domain does not know where the clinic is, the same
    /// reason <c>Vaccination.Record</c> is handed its clinic-local date.
    /// </param>
    public DateTimeOffset SendAt(DateTimeOffset? subjectInstant, DateOnly subjectDate, TimeZoneInfo clinicZone)
    {
        if (OffsetHours is { } hours)
        {
            return (subjectInstant ?? throw new InvalidOperationException("an hour-based rule needs a subject with a time"))
                .AddHours(-hours);
        }

        var local = subjectDate.AddDays(-(OffsetDays ?? 0)).ToDateTime(SendTime ?? DefaultSendTime);

        return new DateTimeOffset(local, clinicZone.GetUtcOffset(local));
    }

    /// <summary>
    /// The span of subject instants whose message falls in <c>(sendFrom, sendTo]</c>, widened by a
    /// day each side for the day-based shape so a reader never misses one at a day boundary. The
    /// exact cut is <see cref="SendAt"/>, applied afterwards.
    /// </summary>
    public (DateTimeOffset From, DateTimeOffset To) SubjectSpan(DateTimeOffset sendFrom, DateTimeOffset sendTo)
    {
        if (OffsetHours is { } hours)
        {
            return (sendFrom.AddHours(hours), sendTo.AddHours(hours));
        }

        var days = OffsetDays ?? 0;

        return (sendFrom.AddDays(days - 1), sendTo.AddDays(days + 1));
    }
}
