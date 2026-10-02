using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.Dtos;

/// <summary><c>GET /clinic</c> — "ชื่อ โลโก้ สี ใบอนุญาต ผู้ควบคุมกิจการ". Public, every field of it.</summary>
public sealed record ClinicProfileDto(
    uint Version,
    string Name,
    string? LogoUrl,
    string? BrandColor,
    string? LicenseNo,
    string? OperatorName)
{
    public static ClinicProfileDto From(ClinicProfile profile) =>
        new(
            profile.Version,
            profile.Name,
            profile.LogoUrl,
            profile.BrandColor,
            profile.LicenseNo,
            profile.OperatorName);
}

/// <summary>One site — <c>GET /clinic/branches</c>, "สาขา ที่อยู่ พิกัด ห้องตรวจ".</summary>
public sealed record BranchDto(
    string Id,
    uint Version,
    string Code,
    string Name,
    string? Address,
    double? Latitude,
    double? Longitude,
    IReadOnlyList<string> ExamRooms)
{
    public static BranchDto From(Branch branch) =>
        new(
            branch.Id,
            branch.Version,
            branch.Code,
            branch.Name,
            branch.Address,
            branch.Latitude,
            branch.Longitude,
            branch.ExamRooms);
}

/// <summary>One weekday. <c>Opens</c> and <c>Closes</c> are <c>HH:mm</c>, absent on a closed day.</summary>
public sealed record DayHoursDto(string Day, string? Opens, string? Closes, bool Closed)
{
    public static DayHoursDto From(DayHours day) =>
        new(
            WireTime.Format(day.Day),
            day.Opens is { } opens ? WireTime.Format(opens) : null,
            day.Closes is { } closes ? WireTime.Format(closes) : null,
            day.IsClosed);
}

public sealed record HolidayDto(string Id, DateOnly Date, string Name, string? BranchId)
{
    public static HolidayDto From(ClinicHoliday holiday) =>
        new(holiday.Id, holiday.Date, holiday.Name, holiday.BranchId);
}

/// <summary>
/// <c>GET /clinic/hours</c> — "เวลาทำการรายวัน วันหยุดประจำสัปดาห์".
/// <para>
/// Upcoming holidays ride along. The contract declares them with <c>POST /clinic/holidays</c> and
/// gives no endpoint that reads them back, and "are you open on the 13th" is the same question as
/// "are you open on Sundays" to the person asking it.
/// </para>
/// <para>
/// <c>Configured</c> is false until someone sets the week, and <c>Days</c> is empty with it — so a
/// client shows "hours not published" rather than a week nobody chose.
/// </para>
/// </summary>
public sealed record OpeningHoursDto(
    uint Version,
    bool Configured,
    IReadOnlyList<DayHoursDto> Days,
    IReadOnlyList<HolidayDto> Holidays)
{
    public static OpeningHoursDto From(OpeningHours hours, IReadOnlyList<ClinicHoliday> holidays) =>
        new(
            hours.Version,
            hours.IsConfigured,
            [.. hours.Days.Select(DayHoursDto.From)],
            [.. holidays.Select(HolidayDto.From)]);
}
