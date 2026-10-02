using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Dtos;

/// <summary>One dose as the API returns it.</summary>
public sealed record VaccinationDto(
    string Id,
    string PetId,
    string VaccineName,
    string? Manufacturer,
    string LotNumber,
    DateOnly VialExpiresOn,
    DateTimeOffset AdministeredAt,
    string AdministeredByStaffId,
    string? ConsultationId,
    DateOnly? NextDueOn,
    string? Note)
{
    public static VaccinationDto From(Vaccination dose) =>
        new(
            dose.Id,
            dose.PetId,
            dose.VaccineName,
            dose.Manufacturer,
            dose.LotNumber,
            dose.VialExpiresOn,
            dose.AdministeredAt,
            dose.AdministeredByStaffId,
            dose.ConsultationId,
            dose.NextDueOn,
            dose.Note);
}

/// <summary>
/// The next dose of one vaccine — "รอบถัดไปที่ครบกำหนด". <c>status</c> is <c>overdue</c>,
/// <c>due_soon</c> (within 30 days) or <c>upcoming</c>; <c>last_dose_id</c> is the dose this due
/// date comes from.
/// </summary>
public sealed record VaccinationDueDto(
    string VaccineName,
    DateOnly NextDueOn,
    string Status,
    string LastDoseId,
    DateTimeOffset LastGivenAt);

/// <summary>
/// <c>GET /staff/pets/{id}/vaccinations</c> — "รอบที่ฉีดแล้วและรอบถัดไปที่ครบกำหนด". The two halves of
/// that line, as two lists.
/// </summary>
public sealed record PetVaccinationsDto(
    string PetId,
    IReadOnlyList<VaccinationDto> Doses,
    IReadOnlyList<VaccinationDueDto> Due);

/// <summary>One point on the weight graph.</summary>
public sealed record WeightPointDto(DateTimeOffset RecordedAt, decimal WeightKg, string ConsultationId);

/// <summary><c>GET /staff/pets/{id}/weights</c> — "ชุดข้อมูลกราฟน้ำหนักย้อนหลัง", oldest first, as a graph reads.</summary>
public sealed record WeightSeriesDto(string PetId, string Unit, IReadOnlyList<WeightPointDto> Points);
