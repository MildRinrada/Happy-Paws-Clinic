using System.Globalization;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.StaffAi;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Laboratory;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Prescriptions;
using HappyPaws.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.StaffAi;

/// <summary>
/// Compact reads of a patient's file for the staff AI. Each list is the newest few by an indexed
/// date column, so the prompt and the cost stay bounded however long the patient's history is.
/// </summary>
internal sealed class StaffAiReader(HappyPawsDbContext db, IClock clock) : IStaffAiReader
{
    public async Task<PetFile?> PetFileAsync(string petId, DateOnly today, CancellationToken cancellationToken = default)
    {
        var pet = await db.Set<Pet>().AsNoTracking().FirstOrDefaultAsync(found => found.Id == petId, cancellationToken);

        if (pet is null)
        {
            return null;
        }

        var carerIds = await db.Set<PetOwnership>().AsNoTracking()
            .Where(link => link.PetId == pet.Id)
            .Select(link => link.CustomerId)
            .ToListAsync(cancellationToken);

        var people = await db.Set<Customer>().AsNoTracking()
            .Where(customer => customer.Id == pet.PrimaryCustomerId || carerIds.Contains(customer.Id))
            .Select(customer => new PersonDetails(customer.DisplayName, customer.Phone, customer.Email))
            .ToListAsync(cancellationToken);

        var allergies = await db.Set<PetAllergy>().AsNoTracking()
            .Where(allergy => allergy.PetId == pet.Id && allergy.WithdrawnAt == null)
            .OrderByDescending(allergy => allergy.RecordedAt)
            .ToListAsync(cancellationToken);

        var visits = await db.Set<Consultation>().AsNoTracking()
            .Where(visit => visit.PetId == pet.Id)
            .OrderByDescending(visit => visit.OpenedAt)
            .Take(PetFileLimits.Visits)
            .Select(visit => new { visit.Id, visit.OpenedAt, visit.ChiefComplaint, visit.Assessment })
            .ToListAsync(cancellationToken);

        var visitIds = visits.Select(visit => visit.Id).ToList();
        var diagnoses = await db.Set<Diagnosis>().AsNoTracking()
            .Where(diagnosis => visitIds.Contains(diagnosis.ConsultationId))
            .Select(diagnosis => new { diagnosis.ConsultationId, diagnosis.Description, diagnosis.IsPrimary })
            .ToListAsync(cancellationToken);

        var medicines = await db.Set<Prescription>().AsNoTracking()
            .Where(medicine => medicine.PetId == pet.Id)
            .OrderByDescending(medicine => medicine.PrescribedAt)
            .Take(PetFileLimits.Medicines)
            .ToListAsync(cancellationToken);

        var weights = await db.Set<VitalsReading>().AsNoTracking()
            .Where(vitals => vitals.PetId == pet.Id && vitals.WeightKg != null)
            .OrderByDescending(vitals => vitals.RecordedAt)
            .Take(PetFileLimits.Weights)
            .Select(vitals => new { vitals.ConsultationId, vitals.RecordedAt, vitals.WeightKg })
            .ToListAsync(cancellationToken);

        var labs = await db.Set<LabRequest>().AsNoTracking()
            .Include(lab => lab.Results)
            .Where(lab => lab.PetId == pet.Id)
            .OrderByDescending(lab => lab.RequestedAt)
            .Take(PetFileLimits.LabRequests)
            .ToListAsync(cancellationToken);

        var vaccinations = await db.Set<Vaccination>().AsNoTracking()
            .Where(dose => dose.PetId == pet.Id)
            .OrderByDescending(dose => dose.AdministeredAt)
            .Take(PetFileLimits.Vaccinations)
            .ToListAsync(cancellationToken);

        return new PetFile(
            new PetFacts(
                pet.Id,
                pet.Name,
                pet.Species,
                pet.Breed,
                WireEnum.Of(pet.Sex),
                pet.Neutered,
                pet.BirthDate,
                pet.BirthDateEstimated,
                pet.IsDeceased),
            people,
            [.. allergies.Select(allergy => new AllergyFact(
                allergy.Id, Day(allergy.RecordedAt), allergy.Substance, allergy.Severity.ToString().ToLowerInvariant(), allergy.Reaction))],
            [.. visits.Select(visit => new VisitFact(
                visit.Id,
                Day(visit.OpenedAt),
                visit.ChiefComplaint,
                visit.Assessment,
                [.. diagnoses
                    .Where(diagnosis => diagnosis.ConsultationId == visit.Id)
                    .OrderByDescending(diagnosis => diagnosis.IsPrimary)
                    .Select(diagnosis => new DiagnosisFact(diagnosis.Description, diagnosis.IsPrimary))]))],
            [.. medicines.Select(medicine => new MedicineFact(
                medicine.Id,
                Day(medicine.PrescribedAt),
                medicine.DrugName,
                medicine.Dose,
                medicine.Frequency,
                medicine.DurationDays,
                medicine.Instructions))],
            [.. weights.Select(weight => new WeightFact(weight.ConsultationId, Day(weight.RecordedAt), weight.WeightKg!.Value))],
            [.. labs.Select(lab => new LabFact(
                lab.Id,
                Day(lab.ResultedAt ?? lab.RequestedAt),
                [.. lab.Tests.Select(test => test.Name)],
                lab.Results.Count(result => !result.IsVoided),
                [.. lab.Results
                    .Where(result => result.IsAbnormal)
                    .Select(result => new LabValueFact(
                        result.Analyte,
                        result.ValueNumeric?.ToString("0.###", CultureInfo.InvariantCulture) ?? result.ValueText ?? string.Empty,
                        result.Unit,
                        Range(result),
                        result.Flag!.Value.ToString().ToLowerInvariant()))]))],
            [.. vaccinations.Select(dose => new VaccinationFact(dose.Id, Day(dose.AdministeredAt), dose.VaccineName, dose.NextDueOn))]);
    }

    public Task<PersonDetails?> CustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        db.Set<Customer>().AsNoTracking()
            .Where(customer => customer.Id == customerId)
            .Select(customer => new PersonDetails(customer.DisplayName, customer.Phone, customer.Email))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> StaffNamesAsync(CancellationToken cancellationToken = default) =>
        await (from membership in db.Set<StaffMembership>().AsNoTracking()
               join account in db.Set<UserAccount>().AsNoTracking() on membership.UserId equals account.Id
               select account.DisplayName)
            .Distinct()
            .ToListAsync(cancellationToken);

    private DateOnly Day(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, clock.ClinicTimeZone).DateTime);

    private static string? Range(LabResult result)
    {
        if (result.ReferenceLow is { } low && result.ReferenceHigh is { } high)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{low:0.###}-{high:0.###}");
        }

        return result.ReferenceText;
    }
}
