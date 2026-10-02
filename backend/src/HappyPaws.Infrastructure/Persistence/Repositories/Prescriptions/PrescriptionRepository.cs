using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Prescriptions;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Prescriptions;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Prescriptions;

/// <summary>
/// Prescriptions read on their own, outside the consultation aggregate that writes them. The branch
/// and the customer are joined in from the consultation; a prescription does not copy either.
/// </summary>
internal sealed class PrescriptionRepository(HappyPawsDbContext db) : IPrescriptionRepository
{
    public async Task<PrescriptionInContext?> FindTrackedAsync(
        string prescriptionId,
        CancellationToken cancellationToken = default)
    {
        var prescription = await db.Set<Prescription>()
            .FirstOrDefaultAsync(item => item.Id == prescriptionId, cancellationToken);

        if (prescription is null)
        {
            return null;
        }

        var visit = await db.Set<Consultation>()
            .AsNoTracking()
            .Where(consultation => consultation.Id == prescription.ConsultationId)
            .Select(consultation => new { consultation.BranchId, consultation.CustomerId })
            .FirstAsync(cancellationToken);

        return new PrescriptionInContext(prescription, visit.BranchId, visit.CustomerId);
    }

    public async Task<CursorPage<Prescription>> ListAsync(
        PrescriptionFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<Prescription>().AsNoTracking().AsQueryable();

        if (filter.PetId is { Length: > 0 } petId)
        {
            query = query.Where(prescription => prescription.PetId == petId);
        }

        if (filter.Dispensed is { } dispensed)
        {
            query = dispensed
                ? query.Where(prescription => prescription.DispensedAt != null)
                : query.Where(prescription => prescription.DispensedAt == null);
        }

        if (filter.BranchIds is { } visible)
        {
            query = query.Where(prescription => db.Set<Consultation>()
                .Any(consultation => consultation.Id == prescription.ConsultationId
                    && visible.Contains(consultation.BranchId)));
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);

            query = query.Where(prescription =>
                prescription.PrescribedAt < sortKey
                || (prescription.PrescribedAt == sortKey && string.Compare(prescription.Id, id) < 0));
        }

        var limit = page.SafeLimit;

        var rows = await query
            .OrderByDescending(prescription => prescription.PrescribedAt).ThenByDescending(prescription => prescription.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var data = hasMore ? rows[..limit] : rows;

        return new CursorPage<Prescription>(
            data, hasMore ? Cursor.Encode(data[^1].PrescribedAt, data[^1].Id) : null);
    }

    public async Task<IReadOnlyList<Prescription>> ForPetsAsync(
        IReadOnlyCollection<string> petIds,
        CancellationToken cancellationToken = default) =>
        petIds.Count == 0
            ? []
            : await db.Set<Prescription>()
                .AsNoTracking()
                .Where(prescription => petIds.Contains(prescription.PetId))
                .ToListAsync(cancellationToken);
}
