using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Patients;

/// <summary>
/// One dose given — <c>POST /staff/pets/{id}/vaccinations</c>,
/// "ล็อตวัคซีน วันหมดอายุ · ตั้งรอบถัดไปอัตโนมัติ".
/// <para>
/// The lot number and the vial's expiry date are required, not optional extras. They are what a
/// clinic needs on the day a manufacturer recalls a batch: "which patients had lot X" has to be a
/// query, not a search through paper.
/// </para>
/// <para>
/// A dose is never edited. A wrong entry is a clinical fact worth keeping, the same reasoning that
/// keeps allergies withdrawn rather than deleted, so correcting one is a new dose with the right
/// details.
/// </para>
/// </summary>
public sealed class Vaccination : Entity
{
    /// <summary>When nobody says otherwise, the next dose is due a year later. Most core vaccines are annual.</summary>
    public const int DefaultValidForDays = 365;

    /// <summary>Three years, the longest interval any common small-animal vaccine is licensed for.</summary>
    public const int MaxValidForDays = 1_095;

    private Vaccination()
    {
    }

    public string PetId { get; private set; } = string.Empty;

    /// <summary>As the clinic writes it — <c>Rabies</c>, <c>DHPPi+L</c>, <c>FVRCP</c>.</summary>
    public string VaccineName { get; private set; } = string.Empty;

    public string? Manufacturer { get; private set; }

    public string LotNumber { get; private set; } = string.Empty;

    /// <summary>The expiry printed on the vial, not when the protection runs out.</summary>
    public DateOnly VialExpiresOn { get; private set; }

    public DateTimeOffset AdministeredAt { get; private set; }

    public string AdministeredByStaffId { get; private set; } = string.Empty;

    /// <summary>The visit it was given in, when there was one.</summary>
    public string? ConsultationId { get; private set; }

    /// <summary>
    /// "ตั้งรอบถัดไปอัตโนมัติ" — the clinic-local date the next dose is due. Null only when the vet
    /// said this vaccine needs no further dose.
    /// </summary>
    public DateOnly? NextDueOn { get; private set; }

    public string? Note { get; private set; }

    /// <summary>
    /// Records a dose.
    /// </summary>
    /// <param name="petId">The patient.</param>
    /// <param name="vaccineName">As the clinic writes it.</param>
    /// <param name="lotNumber">From the vial.</param>
    /// <param name="vialExpiresOn">From the vial.</param>
    /// <param name="administeredAt">When it was given.</param>
    /// <param name="administeredOn">
    /// The clinic-local date of <paramref name="administeredAt"/>. Passed in rather than computed,
    /// because the domain does not know which time zone the clinic is in.
    /// </param>
    /// <param name="administeredByStaffId">Who gave it.</param>
    /// <param name="nextDueOn">An explicit due date. Wins over <paramref name="validForDays"/>.</param>
    /// <param name="validForDays">
    /// How long this dose protects for. Null with no <paramref name="nextDueOn"/> means a year;
    /// zero means no further dose is due.
    /// </param>
    /// <param name="manufacturer">Optional.</param>
    /// <param name="consultationId">The visit, when it was given in one.</param>
    /// <param name="note">Anything worth keeping — the injection site, a reaction.</param>
    public static Vaccination Record(
        string petId,
        string vaccineName,
        string lotNumber,
        DateOnly vialExpiresOn,
        DateTimeOffset administeredAt,
        DateOnly administeredOn,
        string administeredByStaffId,
        DateOnly? nextDueOn = null,
        int? validForDays = null,
        string? manufacturer = null,
        string? consultationId = null,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(vaccineName))
        {
            throw Invalid("ต้องระบุชื่อวัคซีน");
        }

        if (string.IsNullOrWhiteSpace(lotNumber))
        {
            throw Invalid("ต้องระบุเลขล็อตวัคซีน");
        }

        // A vial past its date has lost potency, and a patient vaccinated from it is not protected.
        // Refused rather than recorded with a warning: the record would claim cover that is not there.
        if (vialExpiresOn < administeredOn)
        {
            throw Invalid("วัคซีนล็อตนี้หมดอายุแล้วในวันที่ฉีด");
        }

        if (nextDueOn is { } due && due <= administeredOn)
        {
            throw Invalid("วันครบกำหนดรอบถัดไปต้องอยู่หลังวันที่ฉีด");
        }

        if (validForDays is < 0 or > MaxValidForDays)
        {
            throw Invalid($"ระยะคุ้มกันต้องอยู่ระหว่าง 0 ถึง {MaxValidForDays} วัน");
        }

        return new Vaccination
        {
            Id = EntityId.New(EntityId.Vaccination),
            PetId = petId,
            VaccineName = vaccineName.Trim(),
            Manufacturer = Trimmed(manufacturer),
            LotNumber = lotNumber.Trim(),
            VialExpiresOn = vialExpiresOn,
            AdministeredAt = administeredAt,
            AdministeredByStaffId = administeredByStaffId,
            ConsultationId = consultationId,
            NextDueOn = nextDueOn ?? NextFrom(administeredOn, validForDays),
            Note = Trimmed(note),
        };
    }

    /// <summary>Whether two doses are of the same vaccine, so the later one supersedes the earlier's due date.</summary>
    public bool IsSameVaccineAs(Vaccination other) =>
        string.Equals(VaccineName, other.VaccineName, StringComparison.OrdinalIgnoreCase);

    private static DateOnly? NextFrom(DateOnly administeredOn, int? validForDays) =>
        (validForDays ?? DefaultValidForDays) switch
        {
            0 => null,
            var days => administeredOn.AddDays(days),
        };

    private static DomainException Invalid(string message) => new("invalid_vaccination", message);

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
