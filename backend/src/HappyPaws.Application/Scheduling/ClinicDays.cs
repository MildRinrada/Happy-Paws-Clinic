using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.Scheduling;

/// <summary>
/// Clinic calendar days as instants. <c>?date=2026-10-05</c> means that day in Bangkok, whatever
/// the caller's own clock says.
/// </summary>
public sealed class ClinicDays(IClock clock)
{
    /// <summary>Midnight at the start of a clinic day.</summary>
    public DateTimeOffset StartOf(DateOnly date)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue);

        return new DateTimeOffset(midnight, clock.ClinicTimeZone.GetUtcOffset(midnight));
    }

    /// <summary>The clinic's today.</summary>
    public DateOnly Today => DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);

    /// <summary>The clinic day an instant falls on. 23:30 UTC on the 4th is the 5th in Bangkok.</summary>
    public DateOnly DateOf(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, clock.ClinicTimeZone).DateTime);
}
