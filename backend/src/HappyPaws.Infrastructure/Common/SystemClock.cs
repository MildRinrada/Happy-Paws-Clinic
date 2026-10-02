using HappyPaws.Application.Common.Abstractions;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Common;

/// <summary>
/// The real clock. Tests substitute a fixed one so booking cut-offs, hold expiry and reminder
/// windows are assertable instead of flaky.
/// </summary>
public sealed class SystemClock : IClock
{
    private readonly TimeZoneInfo _clinicTimeZone;

    public SystemClock(IOptions<ClinicOptions> options)
    {
        var id = options.Value.TimeZone;

        // Fail at startup rather than on the first booking request. A clinic running with the
        // wrong zone silently offers the wrong slots all day.
        _clinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public TimeZoneInfo ClinicTimeZone => _clinicTimeZone;

    public DateTimeOffset NowInClinicTime() => TimeZoneInfo.ConvertTime(UtcNow, _clinicTimeZone);
}

/// <summary>Instance-wide clinic settings that are configuration, not data.</summary>
public sealed class ClinicOptions
{
    public const string SectionName = "Clinic";

    /// <summary>IANA id. <c>Asia/Bangkok</c> per the contract.</summary>
    public string TimeZone { get; init; } = "Asia/Bangkok";

    public string DefaultCulture { get; init; } = "th-TH";
}
