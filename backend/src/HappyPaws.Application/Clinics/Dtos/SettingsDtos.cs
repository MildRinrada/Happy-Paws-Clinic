using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.Dtos;

/// <summary>
/// <c>GET</c> / <c>PATCH /clinic/settings/booking</c> — "ช่วงเวลาจอง lead time โควตา walk-in" and
/// "slot 15/20/30 นาที · ยกเลิกล่วงหน้ากี่ชั่วโมง".
/// </summary>
public sealed record BookingSettingsDto(
    uint Version,
    int SlotMinutes,
    int LeadTimeMinutes,
    int CancellationCutoffHours,
    int MaxAdvanceDays,
    int WalkInSlotsPerHour)
{
    public static BookingSettingsDto From(BookingSettings settings) =>
        new(
            settings.Version,
            settings.SlotMinutes,
            settings.LeadTimeMinutes,
            settings.CancellationCutoffHours,
            settings.MaxAdvanceDays,
            settings.WalkInSlotsPerHour);
}

/// <summary>
/// <c>PATCH /clinic/settings/queue</c> — "รูปแบบเลขคิว การรีเซ็ตรายวัน". <c>Example</c> is the
/// fourteenth ticket of a day under these settings, so a settings screen can show what it just
/// chose without reimplementing the format.
/// </summary>
public sealed record QueueSettingsDto(
    uint Version,
    string NumberPrefix,
    int NumberDigits,
    bool ResetDaily,
    string Example)
{
    public static QueueSettingsDto From(QueueSettings settings) =>
        new(
            settings.Version,
            settings.NumberPrefix,
            settings.NumberDigits,
            settings.ResetDaily,
            settings.Format(14));
}

/// <summary>
/// <c>PATCH /clinic/settings/medical-record</c> — "ฟิลด์บังคับ แบบฟอร์มตรวจ การ sign".
/// </summary>
public sealed record MedicalRecordSettingsDto(
    uint Version,
    IReadOnlyList<string> RequiredFields,
    IReadOnlyList<string> ExaminationSystems,
    IReadOnlyList<string> OwnerVisibleSections,
    bool SignerMustBeCaseVet)
{
    public static MedicalRecordSettingsDto From(MedicalRecordSettings settings) =>
        new(
            settings.Version,
            settings.RequiredFields,
            settings.ExaminationSystems,
            settings.OwnerVisibleSections,
            settings.SignerMustBeCaseVet);
}
