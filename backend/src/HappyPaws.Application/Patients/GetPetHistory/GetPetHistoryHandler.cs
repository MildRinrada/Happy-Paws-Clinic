using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Patients.GetPetHistory;

/// <summary>
/// <c>GET /staff/pets/{id}/history</c> — "ไทม์ไลน์รวมทุกเคส วัคซีน ยา แล็บ".
/// </summary>
public sealed record GetPetHistoryQuery(
    string PetId,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// A patient's clinical timeline.
/// <para>
/// <b>This read is audited.</b> It carries <c>record:read</c>, not <c>patient:read</c>, and the
/// difference is the whole point: the registry entry is an identity card, this is the file. Every
/// call writes <c>pet.history_viewed</c> before returning, readable through
/// <c>GET /staff/audit-logs/access</c> — which is what makes "who has been looking at this
/// patient" answerable rather than a promise.
/// </para>
/// <para>
/// That action has been in <c>AuditActions</c> since the audit registry was written, with nothing
/// calling it. The list was declared as a <i>requirement</i> rather than an inventory precisely so
/// that this endpoint could not arrive without it.
/// </para>
/// <para>
/// All four kinds the contract names are here: <c>visit</c>, <c>medicine</c>, <c>vaccination</c>
/// and <c>lab</c>. The last two arrived after the first two, as two more <c>kind</c> values, with
/// no change to this handler or to any client that reads it.
/// </para>
/// </summary>
public sealed class GetPetHistoryHandler(
    IPetRepository pets,
    IPetTimelineReader timeline,
    IAuditTrail audit)
{
    public async Task<CursorPage<PetTimelineEntryDto>> HandleAsync(
        GetPetHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var pet = await pets.FindByIdAsync(query.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", query.PetId);

        var page = await timeline.TimelineAsync(
            pet.Id, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        // Written before the response leaves, and on its own transaction: a read has nothing else
        // to save, and the view is the fact this log exists to capture.
        await audit.RecordImmediatelyAsync(
            new AuditEvent(AuditActions.PetHistoryViewed, AuditResources.Pet, pet.Id, pet.Id),
            cancellationToken);

        return page;
    }
}
