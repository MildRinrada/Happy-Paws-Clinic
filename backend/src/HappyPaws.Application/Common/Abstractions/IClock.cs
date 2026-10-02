namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// Time source for scheduling, booking cut-offs, hold expiry and reminder windows.
/// <para>
/// Injected rather than read from <c>DateTimeOffset.Now</c> so that slot arithmetic and
/// cancellation policies are testable. The clinic operates in <c>Asia/Bangkok</c>; timestamps
/// cross the API as ISO 8601 with offset (<c>2026-08-22T10:30:00+07:00</c>).
/// </para>
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>The clinic's local time zone — <c>Asia/Bangkok</c> in production.</summary>
    TimeZoneInfo ClinicTimeZone { get; }

    DateTimeOffset NowInClinicTime();
}
