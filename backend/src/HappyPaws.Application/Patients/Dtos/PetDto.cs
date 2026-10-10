using HappyPaws.Application.Common;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Dtos;

/// <summary>
/// A patient as the registry lists them. Nickname, photo, diet and behaviour are what the owner
/// keeps up to date from the portal; the behaviour note is what the nurse wants before opening the
/// carrier. The last four fields are staff's and not in the API reference: <c>sex</c> is
/// <c>male</c>, <c>female</c> or <c>unknown</c>, <c>neutered</c> is null when nobody knows. See
/// <c>docs/contract-extensions.md § 28</c>.
/// </summary>
public sealed record PetSummaryDto(
    string Id,
    uint Version,
    string HospitalNumber,
    string Name,
    string Species,
    string? Breed,
    string? MicrochipNumber,
    string PrimaryCustomerId,
    bool IsDeceased,
    DateTimeOffset? DeceasedAt,
    string? Nickname = null,
    string? PhotoDocumentId = null,
    string? Diet = null,
    string? Behaviour = null,
    string Sex = "unknown",
    DateOnly? BirthDate = null,
    bool BirthDateEstimated = false,
    bool? Neutered = null)
{
    public static PetSummaryDto From(Pet pet) =>
        new(
            pet.Id,
            pet.Version,
            pet.HospitalNumber,
            pet.Name,
            pet.Species,
            pet.Breed,
            pet.MicrochipNumber,
            pet.PrimaryCustomerId,
            pet.IsDeceased,
            pet.DeceasedAt,
            pet.Nickname,
            pet.PhotoDocumentId,
            pet.Diet,
            pet.Behaviour,
            WireEnum.Of(pet.Sex),
            pet.BirthDate,
            pet.BirthDateEstimated,
            pet.Neutered);
}

/// <summary>
/// One patient in full — <c>GET /staff/pets/{id}</c>,
/// "สายพันธุ์ น้ำหนักล่าสุด แพ้ยา ไมโครชิป".
/// <para>
/// The contract names exactly four things and this returns exactly those four, plus the carer list
/// that decides who may see any of it. The latest weight comes from the newest vitals reading
/// rather than a column on the patient: the weight graph reads the same rows, and a cached "current
/// weight" would be a second answer able to disagree with the graph beside it.
/// </para>
/// </summary>
public sealed record PetDetailDto(
    PetSummaryDto Pet,
    WeightDto? LatestWeight,
    IReadOnlyList<AllergyDto> Allergies,
    IReadOnlyList<CarerDto> Carers);

/// <summary>The newest recorded weight, and when it was taken.</summary>
public sealed record WeightDto(decimal WeightKg, DateTimeOffset RecordedAt);

/// <summary>
/// One event on a patient's timeline — <c>GET /staff/pets/{id}/history</c>,
/// "ไทม์ไลน์รวมทุกเคส วัคซีน ยา แล็บ".
/// <para>
/// One shape for every kind of event rather than a union per kind, because the thing being asked
/// for is a chronology: the reader wants to scroll a patient's life in order, and four differently
/// shaped arrays they have to interleave themselves is not that. <c>kind</c> says what it was and
/// <c>reference_id</c> is what to open next.
/// </para>
/// </summary>
public sealed record PetTimelineEntryDto(
    string Kind,
    string ReferenceId,
    DateTimeOffset OccurredAt,
    string Summary,
    string? StaffId,
    string? MedicalRecordId);

/// <summary>An allergy as the API returns it — "แสดงเป็นแถบเตือนแดงทุกหน้าที่เกี่ยวข้อง".</summary>
public sealed record AllergyDto(
    string Id,
    string PetId,
    string Substance,
    string Severity,
    string? Reaction,
    DateTimeOffset RecordedAt,
    string RecordedByStaffId)
{
    public static AllergyDto From(PetAllergy allergy) =>
        new(
            allergy.Id,
            allergy.PetId,
            allergy.Substance,
            allergy.Severity.ToString().ToLowerInvariant(),
            allergy.Reaction,
            allergy.RecordedAt,
            allergy.RecordedByStaffId);
}

/// <summary>
/// A carer link — "เพิ่มผู้ดูแลร่วม · กำหนดสิทธิ์ดูประวัติ".
/// <para>
/// Revoked links are returned too, with <c>revoked_at</c> set. "ถอนสิทธิ์เข้าถึง ไม่ลบประวัติ"
/// means the link is closed, not erased, and who could see what until when stays answerable.
/// </para>
/// </summary>
public sealed record CarerDto(
    string Id,
    string CustomerId,
    bool IsPrimary,
    bool MayViewHistory,
    DateTimeOffset? RevokedAt)
{
    public static CarerDto From(PetOwnership ownership) =>
        new(
            ownership.Id,
            ownership.CustomerId,
            ownership.IsPrimary,
            ownership.MayViewHistory,
            ownership.RevokedAt);
}
