using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Prescriptions.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.Application.Prescriptions.ListPrescriptions;

/// <summary>
/// <c>GET /staff/prescriptions?pet_id=</c> — "ยาที่เคยจ่ายทั้งหมด · ตรวจซ้ำซ้อน". <c>Dispensed</c>
/// false is what the pharmacy still has to hand over.
/// </summary>
public sealed record ListPrescriptionsQuery(
    string? PetId = null,
    bool? Dispensed = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// A patient's medication history, each entry checked against the others.
/// <para>
/// <b>"ตรวจซ้ำซ้อน" is answered per entry.</b> Each prescription lists the others of the same drug
/// whose courses overlap it, checked against the patient's whole history rather than the page
/// being returned. A duplicate on page two is still a duplicate of something on page one.
/// </para>
/// <para>
/// With <c>pet_id</c> the list is that patient's whole history across every branch, and reading it
/// is written to the access log like the timeline. Without it the list is the clinic's, restricted
/// to the caller's branches, which with <c>?dispensed=false</c> is the pharmacy's work queue.
/// </para>
/// </summary>
public sealed class ListPrescriptionsHandler(
    IPrescriptionRepository prescriptions,
    IPetRepository pets,
    BranchGuard branchGuard,
    IAuditTrail audit,
    IClock clock)
{
    public async Task<CursorPage<PrescriptionItemDto>> HandleAsync(
        ListPrescriptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.PetId is { Length: > 0 } petId)
        {
            _ = await pets.FindByIdAsync(petId, cancellationToken)
                ?? throw new NotFoundException("pet", petId);
        }

        var forOnePet = query.PetId is { Length: > 0 };

        var page = await prescriptions.ListAsync(
            new PrescriptionFilter(query.PetId, query.Dispensed, forOnePet ? null : branchGuard.VisibleBranchIds),
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        var history = await prescriptions.ForPetsAsync(
            [.. page.Data.Select(prescription => prescription.PetId).Distinct()],
            cancellationToken);

        var now = clock.UtcNow;
        var items = page.Data
            .Select(prescription => PrescriptionItemDto.From(prescription, now, DuplicatesOf(prescription, history)))
            .ToList();

        if (forOnePet)
        {
            await audit.RecordImmediatelyAsync(
                new AuditEvent(AuditActions.PrescriptionsViewed, AuditResources.Pet, query.PetId!, query.PetId),
                cancellationToken);
        }

        return new CursorPage<PrescriptionItemDto>(items, page.NextCursor);
    }

    public static IReadOnlyList<string> DuplicatesOf(Prescription prescription, IEnumerable<Prescription> history) =>
    [
        .. history
            .Where(other => other.PetId == prescription.PetId && prescription.DuplicatesTherapyOf(other))
            .OrderBy(other => other.PrescribedAt)
            .Select(other => other.Id),
    ];
}
