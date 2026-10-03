namespace HappyPaws.Api.Contracts.Clinics;

/// <summary>
/// <c>PATCH /clinic</c> — "โปรไฟล์และ branding ของคลินิก". An absent field is left alone; an empty
/// string clears it, except <c>name</c>.
/// </summary>
public sealed record UpdateClinicProfileRequest(
    string? Name = null,
    string? LogoUrl = null,
    string? BrandColor = null,
    string? LicenseNo = null,
    string? OperatorName = null);

/// <summary>
/// <c>POST /clinic/branches</c> — "เปิดสาขาใหม่ในคลินิกเดียวกัน". <c>code</c> becomes the readable
/// half of the id: <c>sukhumvit39</c> → <c>brn_sukhumvit39</c>.
/// </summary>
public sealed record CreateBranchRequest(
    string Code,
    string Name,
    string? Address = null,
    double? Latitude = null,
    double? Longitude = null,
    IReadOnlyList<string>? ExamRooms = null);

/// <summary><c>PUT /clinic/hours</c> — "ตั้งทั้งสัปดาห์ในคำขอเดียว". All seven days, each once.</summary>
public sealed record ReplaceOpeningHoursRequest(IReadOnlyList<DayHoursRequest> Days);

/// <summary>
/// One weekday: <c>{ "day": "monday", "opens": "09:00", "closes": "18:00" }</c>, or
/// <c>{ "day": "sunday", "closed": true }</c> for a weekly closed day.
/// </summary>
public sealed record DayHoursRequest(string Day, string? Opens = null, string? Closes = null, bool Closed = false);

/// <summary>
/// <c>POST /clinic/holidays</c> — "วันหยุดพิเศษ · ปิดรับนัดอัตโนมัติ". Omit <c>branch_id</c> to shut
/// every branch.
/// </summary>
public sealed record AddHolidayRequest(DateOnly Date, string Name, string? BranchId = null);

/// <summary>
/// <c>POST /clinic/contacts</c> — <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 15</c>. <c>kind</c> is one of <c>phone</c>, <c>emergency</c>,
/// <c>email</c>, <c>line</c>, <c>facebook</c>, <c>instagram</c>, <c>website</c>, <c>map</c>,
/// <c>other</c>. <c>embed_url</c> is a Google Maps embed address, for a map only. Omit
/// <c>branch_id</c> for the whole clinic.
/// </summary>
public sealed record AddContactRequest(
    string Kind,
    string Label,
    string Value,
    string? Url = null,
    string? EmbedUrl = null,
    string? BranchId = null,
    int SortOrder = 0);

/// <summary>
/// <c>PATCH /clinic/contacts/{id}</c>. An absent field is left alone; an empty string clears
/// <c>url</c>, <c>embed_url</c> and <c>branch_id</c>.
/// </summary>
public sealed record UpdateContactRequest(
    string? Kind = null,
    string? Label = null,
    string? Value = null,
    string? Url = null,
    string? EmbedUrl = null,
    string? BranchId = null,
    int? SortOrder = null);

/// <summary>
/// <c>PATCH /clinic/settings/booking</c> — "slot 15/20/30 นาที · ยกเลิกล่วงหน้ากี่ชั่วโมง".
/// </summary>
public sealed record UpdateBookingSettingsRequest(
    int? SlotMinutes = null,
    int? LeadTimeMinutes = null,
    int? CancellationCutoffHours = null,
    int? MaxAdvanceDays = null,
    int? WalkInSlotsPerHour = null);

/// <summary>
/// <c>PATCH /clinic/settings/queue</c> — "รูปแบบเลขคิว การรีเซ็ตรายวัน". An empty body reads the
/// settings back unchanged.
/// </summary>
public sealed record UpdateQueueSettingsRequest(
    string? NumberPrefix = null,
    int? NumberDigits = null,
    bool? ResetDaily = null);

/// <summary>
/// <c>PATCH /clinic/settings/medical-record</c> — "ฟิลด์บังคับ แบบฟอร์มตรวจ การ sign". An absent
/// field is left alone, an empty list clears that setting, and an empty body reads the settings
/// back unchanged.
/// </summary>
public sealed record UpdateMedicalRecordSettingsRequest(
    IReadOnlyList<string>? RequiredFields = null,
    IReadOnlyList<string>? ExaminationSystems = null,
    IReadOnlyList<string>? OwnerVisibleSections = null,
    bool? SignerMustBeCaseVet = null);
