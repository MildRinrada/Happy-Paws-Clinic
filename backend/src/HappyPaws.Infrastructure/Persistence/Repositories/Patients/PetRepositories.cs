using System.Globalization;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Laboratory;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Prescriptions;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Patients;

/// <summary>EF Core implementation of the patient port.</summary>
internal sealed class PetRepository(HappyPawsDbContext db) : IPetRepository
{
    public void Add(Pet pet) => db.Set<Pet>().Add(pet);

    public Task<Pet?> FindByIdAsync(string petId, CancellationToken cancellationToken = default) =>
        db.Set<Pet>().AsNoTracking().FirstOrDefaultAsync(pet => pet.Id == petId, cancellationToken);

    public Task<Pet?> FindTrackedByIdAsync(string petId, CancellationToken cancellationToken = default) =>
        db.Set<Pet>().FirstOrDefaultAsync(pet => pet.Id == petId, cancellationToken);

    public Task<bool> MicrochipExistsAsync(string microchipNumber, CancellationToken cancellationToken = default) =>
        db.Set<Pet>().AnyAsync(pet => pet.MicrochipNumber == microchipNumber, cancellationToken);

    public async Task<IReadOnlyList<Pet>> ForCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default) =>
        // Through the carer links rather than `primary_customer_id`: a co-owner's patients are
        // theirs too, and the customer record's "สัตว์ทั้งหมด" means every animal they care for.
        await db.Set<PetOwnership>()
            .AsNoTracking()
            .Where(link => link.CustomerId == customerId && link.RevokedAt == null)
            .Join(db.Set<Pet>().AsNoTracking(), link => link.PetId, pet => pet.Id, (_, pet) => pet)
            .OrderBy(pet => pet.Name)
            .ToListAsync(cancellationToken);

    public async Task<CursorPage<Pet>> ListAsync(
        PetListFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<Pet>().AsNoTracking().AsQueryable();

        if (!filter.IncludeDeceased)
        {
            query = query.Where(pet => pet.DeceasedAt == null);
        }

        if (filter.CustomerId is { Length: > 0 } customerId)
        {
            query = query.Where(pet => db.Set<PetOwnership>()
                .Any(link => link.PetId == pet.Id && link.CustomerId == customerId && link.RevokedAt == null));
        }

        if (filter.Species is { Length: > 0 } species)
        {
            query = query.Where(pet => pet.Species == species);
        }

        if (filter.Query is { Length: > 0 } text)
        {
            var pattern = $"%{text}%";

            // The three things printed on a collar tag or read off a vaccination book.
            query = query.Where(pet =>
                EF.Functions.ILike(pet.Name, pattern)
                || EF.Functions.ILike(pet.HospitalNumber, pattern)
                || (pet.MicrochipNumber != null && EF.Functions.ILike(pet.MicrochipNumber, pattern)));
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);

            query = query.Where(pet =>
                pet.CreatedAt < sortKey || (pet.CreatedAt == sortKey && string.Compare(pet.Id, id) < 0));
        }

        var limit = page.SafeLimit;

        var rows = await query
            .OrderByDescending(pet => pet.CreatedAt).ThenByDescending(pet => pet.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var data = hasMore ? rows[..limit] : rows;

        return new CursorPage<Pet>(
            data, hasMore ? Cursor.Encode(data[^1].CreatedAt, data[^1].Id) : null);
    }
}

/// <summary>EF Core implementation of the carer-link port. Every read is tracked: the callers mutate.</summary>
internal sealed class PetOwnershipRepository(HappyPawsDbContext db) : IPetOwnershipRepository
{
    public void Add(PetOwnership ownership) => db.Set<PetOwnership>().Add(ownership);

    public void Remove(PetOwnership ownership) => db.Set<PetOwnership>().Remove(ownership);

    public async Task<IReadOnlyList<PetOwnership>> ForPetAsync(
        string petId,
        CancellationToken cancellationToken = default) =>
        await db.Set<PetOwnership>()
            .Where(link => link.PetId == petId)
            .OrderByDescending(link => link.IsPrimary).ThenBy(link => link.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PetOwnership>> ForCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default) =>
        await db.Set<PetOwnership>()
            .Where(link => link.CustomerId == customerId)
            .ToListAsync(cancellationToken);

    public Task<PetOwnership?> FindActiveAsync(
        string petId,
        string customerId,
        CancellationToken cancellationToken = default) =>
        db.Set<PetOwnership>()
            .FirstOrDefaultAsync(
                link => link.PetId == petId && link.CustomerId == customerId && link.RevokedAt == null,
                cancellationToken);
}

/// <summary>
/// The patient's cross-aggregate reads: last weight, and the timeline.
/// <para>
/// Both are projections built in SQL rather than by loading aggregates, which is what this port
/// exists to keep out of the Pet repository.
/// </para>
/// </summary>
internal sealed class PetTimelineReader(HappyPawsDbContext db) : IPetTimelineReader
{
    public async Task<WeightDto?> LatestWeightAsync(
        string petId,
        CancellationToken cancellationToken = default)
    {
        // Reads the (pet_id, recorded_at) index the weight graph uses — same rows, so the number
        // on the patient card can never disagree with the chart beside it.
        var reading = await db.Set<VitalsReading>()
            .AsNoTracking()
            .Where(vitals => vitals.PetId == petId && vitals.WeightKg != null)
            .OrderByDescending(vitals => vitals.RecordedAt)
            .Select(vitals => new { vitals.WeightKg, vitals.RecordedAt })
            .FirstOrDefaultAsync(cancellationToken);

        return reading is null ? null : new WeightDto(reading.WeightKg!.Value, reading.RecordedAt);
    }

    public async Task<IReadOnlyList<WeightPointDto>> WeightSeriesAsync(
        string petId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int maxPoints,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<VitalsReading>()
            .AsNoTracking()
            .Where(vitals => vitals.PetId == petId && vitals.WeightKg != null);

        if (from is { } start)
        {
            query = query.Where(vitals => vitals.RecordedAt >= start);
        }

        if (to is { } end)
        {
            query = query.Where(vitals => vitals.RecordedAt <= end);
        }

        // Newest first to keep the most recent points when capped, then turned round for the graph.
        var rows = await query
            .OrderByDescending(vitals => vitals.RecordedAt)
            .Take(maxPoints)
            .Select(vitals => new WeightPointDto(vitals.RecordedAt, vitals.WeightKg!.Value, vitals.ConsultationId))
            .ToListAsync(cancellationToken);

        rows.Reverse();

        return rows;
    }

    public async Task<CursorPage<PetTimelineEntryDto>> TimelineAsync(
        string petId,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var limit = page.SafeLimit;
        var before = page.Cursor is { Length: > 0 } cursor ? Cursor.Decode(cursor).SortKey : (DateTimeOffset?)null;

        // Four sources, unioned in memory rather than in SQL. They are different tables with
        // different shapes and no common projection, and the page size caps how much of any can
        // be read — a patient with a thousand visits still costs four indexed reads of `limit + 1`.
        var visits = await VisitsAsync(petId, before, limit + 1, cancellationToken);
        var medicines = await MedicinesAsync(petId, before, limit + 1, cancellationToken);
        var vaccinations = await VaccinationsAsync(petId, before, limit + 1, cancellationToken);
        var labs = await LabsAsync(petId, before, limit + 1, cancellationToken);

        var merged = visits.Concat(medicines).Concat(vaccinations).Concat(labs)
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.ReferenceId)
            .ToList();

        var hasMore = merged.Count > limit;
        var data = hasMore ? merged[..limit] : merged;

        return new CursorPage<PetTimelineEntryDto>(
            data, hasMore ? Cursor.Encode(data[^1].OccurredAt, data[^1].ReferenceId) : null);
    }

    private async Task<List<PetTimelineEntryDto>> VisitsAsync(
        string petId,
        DateTimeOffset? before,
        int take,
        CancellationToken cancellationToken)
    {
        var query = db.Set<Consultation>().AsNoTracking().Where(visit => visit.PetId == petId);

        if (before is { } cutoff)
        {
            query = query.Where(visit => visit.OpenedAt < cutoff);
        }

        var rows = await query
            .OrderByDescending(visit => visit.OpenedAt)
            .Take(take)
            .Select(visit => new
            {
                visit.Id,
                visit.OpenedAt,
                visit.VetStaffId,
                visit.MedicalRecordId,
                Diagnosis = visit.Diagnoses
                    .Where(diagnosis => diagnosis.IsPrimary)
                    .Select(diagnosis => diagnosis.Description)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new PetTimelineEntryDto(
                "visit",
                row.Id,
                row.OpenedAt,
                row.Diagnosis ?? "ยังไม่มีคำวินิจฉัย",
                row.VetStaffId,
                row.MedicalRecordId)),
        ];
    }

    private async Task<List<PetTimelineEntryDto>> MedicinesAsync(
        string petId,
        DateTimeOffset? before,
        int take,
        CancellationToken cancellationToken)
    {
        var query = db.Set<Prescription>().AsNoTracking().Where(medicine => medicine.PetId == petId);

        if (before is { } cutoff)
        {
            query = query.Where(medicine => medicine.PrescribedAt < cutoff);
        }

        var rows = await query
            .OrderByDescending(medicine => medicine.PrescribedAt)
            .Take(take)
            .Select(medicine => new
            {
                medicine.Id,
                medicine.PrescribedAt,
                medicine.PrescribedByStaffId,
                medicine.DrugName,
                medicine.Dose,
                medicine.DurationDays,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new PetTimelineEntryDto(
                "medicine",
                row.Id,
                row.PrescribedAt,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{row.DrugName} {row.Dose} × {row.DurationDays} วัน"),
                row.PrescribedByStaffId,
                null)),
        ];
    }

    private async Task<List<PetTimelineEntryDto>> VaccinationsAsync(
        string petId,
        DateTimeOffset? before,
        int take,
        CancellationToken cancellationToken)
    {
        var query = db.Set<Vaccination>().AsNoTracking().Where(dose => dose.PetId == petId);

        if (before is { } cutoff)
        {
            query = query.Where(dose => dose.AdministeredAt < cutoff);
        }

        var rows = await query
            .OrderByDescending(dose => dose.AdministeredAt)
            .Take(take)
            .Select(dose => new { dose.Id, dose.AdministeredAt, dose.AdministeredByStaffId, dose.VaccineName, dose.LotNumber })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new PetTimelineEntryDto(
                "vaccination",
                row.Id,
                row.AdministeredAt,
                $"{row.VaccineName} (ล็อต {row.LotNumber})",
                row.AdministeredByStaffId,
                null)),
        ];
    }

    private async Task<List<PetTimelineEntryDto>> LabsAsync(
        string petId,
        DateTimeOffset? before,
        int take,
        CancellationToken cancellationToken)
    {
        var query = db.Set<LabRequest>().AsNoTracking().Where(lab => lab.PetId == petId);

        if (before is { } cutoff)
        {
            query = query.Where(lab => lab.RequestedAt < cutoff);
        }

        var rows = await query
            .OrderByDescending(lab => lab.RequestedAt)
            .Take(take)
            .Select(lab => new
            {
                lab.Id,
                lab.RequestedAt,
                lab.RequestedByStaffId,
                lab.Status,
                lab.Tests,
                // A voided value (docs/contract-extensions.md § 52) counts towards nothing.
                Abnormal = lab.Results.Count(result =>
                    result.VoidedAt == null
                    && (result.Flag == LabFlag.Low || result.Flag == LabFlag.High || result.Flag == LabFlag.Abnormal)),
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new PetTimelineEntryDto(
                "lab",
                row.Id,
                row.RequestedAt,
                LabSummary(row.Tests, row.Status, row.Abnormal),
                row.RequestedByStaffId,
                null)),
        ];
    }

    private static string LabSummary(IReadOnlyList<LabTest> tests, LabRequestStatus status, int abnormal)
    {
        var names = string.Join(", ", tests.Select(test => test.Name));

        return status switch
        {
            LabRequestStatus.Pending => $"{names} · รอผล",
            _ when abnormal > 0 => $"{names} · ผิดปกติ {abnormal} ค่า",
            _ => $"{names} · ผลปกติ",
        };
    }
}

/// <summary>
/// Issues <c>HN-2026-0042</c>.
/// <para>
/// The year comes from the clinic's calendar, not UTC — the same reason invoice numbering uses the
/// clinic month. A patient registered at 1 a.m. on New Year's Day in Bangkok must not carry last
/// year's HN.
/// </para>
/// </summary>
internal sealed class HospitalNumberGenerator(HappyPawsDbContext db, IClock clock) : IHospitalNumberGenerator
{
    public async Task<string> NextAsync(
        DateTimeOffset registeredAt,
        CancellationToken cancellationToken = default)
    {
        var year = TimeZoneInfo.ConvertTime(registeredAt, clock.ClinicTimeZone)
            .ToString("yyyy", CultureInfo.InvariantCulture);

        var prefix = $"HN-{year}-";
        var next = await DocumentSequence.NextAsync(
            db, "pets", "hospital_number", prefix, segment: 3, cancellationToken);

        return $"{prefix}{next:D4}";
    }
}
