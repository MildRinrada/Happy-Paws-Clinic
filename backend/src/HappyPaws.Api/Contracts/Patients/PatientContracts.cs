namespace HappyPaws.Api.Contracts.Patients;

/// <summary>
/// <c>POST /staff/pets</c> — "ออก HN อัตโนมัติ · ผูกเจ้าของหลัก".
/// <para>
/// No hospital number: it is issued by the server, which is what "อัตโนมัติ" means. Sex, birth date
/// and neuter status are optional and not in the API reference; see
/// <c>docs/contract-extensions.md § 28</c>. <c>sex</c> is <c>male</c>, <c>female</c> or
/// <c>unknown</c>; <c>birth_date</c> is <c>YYYY-MM-DD</c> and not in the future.
/// </para>
/// </summary>
public sealed record CreatePetRequest(
    string Name,
    string Species,
    string PrimaryCustomerId,
    string? Breed = null,
    string? MicrochipNumber = null,
    string? Sex = null,
    DateOnly? BirthDate = null,
    bool BirthDateEstimated = false,
    bool? Neutered = null);

/// <summary>
/// <c>PATCH /staff/pets/{id}</c>, staff's correction of the identity card. Not in the API
/// reference; see <c>docs/contract-extensions.md § 28</c>.
/// <para>
/// Send only what changes. An empty <c>breed</c> or <c>microchip_number</c> clears it;
/// <c>clear: ["birth_date", "neutered"]</c> sets those back to unknown. Nickname, photo, diet and
/// behaviour are the owner's and are not accepted here.
/// </para>
/// </summary>
public sealed record UpdatePetRequest(
    string? Name = null,
    string? Species = null,
    string? Breed = null,
    string? MicrochipNumber = null,
    string? Sex = null,
    DateOnly? BirthDate = null,
    bool? BirthDateEstimated = null,
    bool? Neutered = null,
    IReadOnlyList<string>? Clear = null);

/// <summary>
/// <c>POST /staff/pets/{id}/deceased/undo</c>, a death recorded by mistake. The reason is required.
/// Not in the API reference; see <c>docs/contract-extensions.md § 29</c>.
/// </summary>
public sealed record UndoDeceasedRequest(string? Reason = null);

/// <summary>
/// <c>POST /staff/pets/{id}/owners</c> — "เพิ่มผู้ดูแลร่วม · กำหนดสิทธิ์ดูประวัติ".
/// <para>
/// <c>may_view_history</c> is the second half of that sentence and defaults to true. Setting it
/// false gives someone the ability to book and collect the animal without reading its medical
/// record — a distinction the contract draws and this honours.
/// </para>
/// </summary>
public sealed record AddCarerRequest(string CustomerId, bool MayViewHistory = true);

/// <summary>
/// <c>POST /staff/pets/{id}/allergies</c> — "แสดงเป็นแถบเตือนแดงทุกหน้าที่เกี่ยวข้อง".
/// <para>
/// <c>severity</c> is <c>mild</c>, <c>moderate</c> or <c>severe</c>. A severe allergy cannot be
/// overridden when prescribing; the other two can, with an acknowledgement.
/// </para>
/// </summary>
public sealed record RecordAllergyRequest(string Substance, string Severity, string? Reaction = null);

/// <summary>
/// <c>POST /staff/pets/{id}/deceased</c> — "ปิดเรกคอร์ดอย่างสุภาพ · หยุดแจ้งเตือนทุกชนิด".
/// <para>
/// <c>occurred_at</c> defaults to now; it is accepted because a record is often closed a day or two
/// after the fact. Future visits already in the calendar are cancelled — a rule this project chose
/// because the contract does not state one, recorded in <c>docs/contract-extensions.md § 6</c>.
/// </para>
/// </summary>
public sealed record MarkDeceasedRequest(DateTimeOffset? OccurredAt = null, string? Note = null);

/// <summary>
/// <c>POST /staff/pets/{id}/documents</c> — "อัปโหลดผ่าน pre-signed URL · เลือกให้เจ้าของเห็นได้".
/// Describes the file to come; the bytes go to the URL in the answer. <c>visible_to_owner</c>
/// defaults to false.
/// </summary>
public sealed record CreatePetDocumentRequest(
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Kind = null,
    string? Title = null,
    bool VisibleToOwner = false,
    string? ConsultationId = null);

/// <summary>
/// <c>POST /staff/pets/{id}/treatment-plans</c> — "แผนหลายครั้ง · ผูกกับนัดและการเตือน".
/// <para>
/// The sessions are <c>rrule</c>, or <c>sessions</c> every <c>interval_days</c>, or the course in
/// the template named by <c>template_id</c>. <c>book</c> defaults to true and books every session.
/// </para>
/// </summary>
public sealed record CreateTreatmentPlanRequest(
    string ServiceId,
    DateTimeOffset FirstStartsAt,
    string? Title = null,
    string? Rrule = null,
    int? Sessions = null,
    int? IntervalDays = null,
    string? TemplateId = null,
    string? ConsultationId = null,
    string? StaffId = null,
    string? BranchId = null,
    string? Notes = null,
    bool Book = true,
    string? Type = null);

/// <summary>
/// <c>POST /staff/pets/{id}/vaccinations</c> — "ล็อตวัคซีน วันหมดอายุ · ตั้งรอบถัดไปอัตโนมัติ".
/// <para>
/// <c>lot_number</c> and <c>vial_expires_on</c> are required: they are what a recall searches by.
/// The next dose is <c>next_due_on</c> when given, otherwise <c>valid_for_days</c> after today,
/// otherwise a year. <c>valid_for_days: 0</c> means no further dose.
/// </para>
/// </summary>
public sealed record RecordVaccinationRequest(
    string VaccineName,
    string LotNumber,
    DateOnly VialExpiresOn,
    DateTimeOffset? AdministeredAt = null,
    DateOnly? NextDueOn = null,
    int? ValidForDays = null,
    string? Manufacturer = null,
    string? ConsultationId = null,
    string? Note = null);
