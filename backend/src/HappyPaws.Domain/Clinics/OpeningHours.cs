using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Clinics;

/// <summary>
/// One day of the week as the clinic keeps it. A day with no times is a weekly closed day
/// ("วันหยุดประจำสัปดาห์").
/// </summary>
public sealed record DayHours(DayOfWeek Day, TimeOnly? Opens, TimeOnly? Closes)
{
    public bool IsClosed => Opens is null || Closes is null;

    public static DayHours Closed(DayOfWeek day) => new(day, null, null);
}

/// <summary>
/// The clinic's weekly opening hours — <c>GET /clinic/hours</c>, "เวลาทำการรายวัน
/// วันหยุดประจำสัปดาห์".
/// <para>
/// One row holding the whole week, because that is the unit the contract writes:
/// <c>PUT /clinic/hours</c> is "ตั้งทั้งสัปดาห์ในคำขอเดียว". Seven separate rows would give seven
/// versions and no way to say "the week changed while you were editing it".
/// </para>
/// <para>
/// <b>These hours are what the clinic tells the public, not what makes a slot bookable.</b> The
/// contract makes the roster the source of bookable time ("ตารางเวรเป็นตัวกำหนด slot"), so an open
/// branch with no vet on shift still offers nothing — see <c>StaffShift</c>. Holidays are the part
/// of clinic setup that does close booking; see <see cref="ClinicHoliday"/>.
/// </para>
/// </summary>
public sealed class OpeningHours : Entity
{
    public const string SingletonId = "set_hours";

    private readonly List<DayHours> _days = [];

    private OpeningHours()
    {
    }

    /// <summary>Seven entries once configured, Monday first. Empty until someone sets the week.</summary>
    public IReadOnlyList<DayHours> Days => _days;

    public bool IsConfigured => _days.Count > 0;

    /// <summary>
    /// Unconfigured, not "open nine to six". A default week would publish opening hours on the
    /// clinic's website that nobody at the clinic chose.
    /// </summary>
    public static OpeningHours CreateDefault() => new() { Id = SingletonId };

    /// <summary>
    /// Replaces the whole week. All seven days must be stated, each exactly once, so a request
    /// that forgets Sunday is refused instead of silently keeping last year's Sunday.
    /// </summary>
    public void ReplaceWeek(IEnumerable<DayHours> days)
    {
        var week = days.ToList();

        if (week.Count != 7 || week.Select(day => day.Day).Distinct().Count() != 7)
        {
            throw new DomainException("invalid_opening_hours", "ต้องระบุเวลาทำการให้ครบทั้ง 7 วัน วันละหนึ่งรายการ");
        }

        foreach (var day in week)
        {
            if ((day.Opens is null) != (day.Closes is null))
            {
                throw new DomainException("invalid_opening_hours", "ต้องระบุทั้งเวลาเปิดและเวลาปิด หรือเว้นทั้งคู่สำหรับวันหยุด");
            }

            if (day is { Opens: { } opens, Closes: { } closes } && closes <= opens)
            {
                throw new DomainException("invalid_opening_hours", "เวลาปิดต้องอยู่หลังเวลาเปิด");
            }
        }

        _days.Clear();

        // Monday first: a Thai clinic's week, and the order every printed timetable uses.
        _days.AddRange(week.OrderBy(day => ((int)day.Day + 6) % 7));
    }
}
