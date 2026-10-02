using System.Globalization;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.MedicalRecords.GenerateDocument;

/// <summary>Everything a printed document may draw on, gathered once by the handler.</summary>
public sealed record DocumentContext(
    Pet Pet,
    Customer? Owner,
    ClinicProfile Clinic,
    Branch? Branch,
    PrintableSignature Signer,
    DateOnly Today,
    string? RecordId,
    ConsultationDto? Visit,
    WeightDto? LatestWeight,
    IReadOnlyList<PetAllergy> Allergies,
    IReadOnlyList<Vaccination> Vaccinations,
    TimeZoneInfo ClinicTimeZone)
{
    private static readonly CultureInfo Thai = CultureInfo.GetCultureInfo("th-TH");

    /// <summary>A Thai date with the Buddhist year, as Thai documents are dated — "2 ตุลาคม 2569".</summary>
    public static string ThaiDate(DateOnly date) => date.ToString("d MMMM yyyy", Thai);

    public string ThaiDate(DateTimeOffset instant) =>
        ThaiDate(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, ClinicTimeZone).DateTime));

    /// <summary>The clinic's licence and the branch's address, under its name in the header.</summary>
    public IReadOnlyList<string> ClinicLines
    {
        get
        {
            var lines = new List<string>();

            if (Clinic.LicenseNo is { } license)
            {
                lines.Add($"ใบอนุญาตสถานพยาบาลสัตว์ {license}");
            }

            if (Branch is not null)
            {
                lines.Add(string.IsNullOrWhiteSpace(Branch.Address) ? Branch.Name : $"{Branch.Name} {Branch.Address}");
            }

            return lines;
        }
    }

    /// <summary>The patient's identity, the same block on every document.</summary>
    public PrintableSection PatientSection() => new(
        "ข้อมูลสัตว์",
        [
            new("ชื่อ", Pet.Name),
            new("HN", Pet.HospitalNumber),
            new("ชนิด", Pet.Species),
            new("สายพันธุ์", Pet.Breed ?? "-"),
            new("ไมโครชิป", Pet.MicrochipNumber ?? "-"),
            new("เจ้าของ", Owner?.DisplayName ?? "-"),
            new("โทรศัพท์เจ้าของ", Owner?.Phone ?? "-"),
        ]);

    public string Weight => LatestWeight is null
        ? "-"
        : string.Create(CultureInfo.InvariantCulture, $"{LatestWeight.WeightKg:0.##} กก. ({ThaiDate(LatestWeight.RecordedAt)})");
}
