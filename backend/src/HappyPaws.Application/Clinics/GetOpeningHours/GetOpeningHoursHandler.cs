using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.Clinics.GetOpeningHours;

/// <summary>
/// <c>GET /clinic/hours</c> — "เวลาทำการรายวัน วันหยุดประจำสัปดาห์", with the special holidays
/// still to come.
/// </summary>
public sealed class GetOpeningHoursHandler(
    IClinicSettingsStore settings,
    IHolidayRepository holidays,
    IClock clock)
{
    /// <summary>A year of special days is every clinic's whole list; the cap only stops a runaway one.</summary>
    private const int MaxHolidays = 100;

    public async Task<OpeningHoursDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var hours = await settings.OpeningHoursAsync(cancellationToken);

        // "Upcoming" is judged in clinic time: at 01:00 Bangkok on the 13th, UTC still says the
        // 12th, and a closure that starts today must not look like it starts tomorrow.
        var today = DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);

        return OpeningHoursDto.From(hours, await holidays.FromAsync(today, MaxHolidays, cancellationToken));
    }
}
