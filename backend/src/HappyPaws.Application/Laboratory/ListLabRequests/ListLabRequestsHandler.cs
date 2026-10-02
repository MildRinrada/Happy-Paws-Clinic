using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.ListLabRequests;

/// <summary>
/// <c>GET /staff/lab-requests?status=pending</c> — "งานค้างของหมอ · ผลที่ยังไม่มีคนอ่าน".
/// <c>Status</c> is <c>pending</c>, <c>resulted</c> (not yet read) or <c>reviewed</c>, null for all;
/// <c>RequestedBy</c> is a staff id, or <c>me</c> for the caller's own requests.
/// </summary>
public sealed record ListLabRequestsQuery(
    string? Status = null,
    string? PetId = null,
    string? RequestedBy = null,
    string? BranchId = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// The lab worklist.
/// <para>
/// The two halves of the contract's description are two statuses: <c>pending</c> is waiting on the
/// lab, <c>resulted</c> is waiting on a vet. <c>?requested_by=me</c> narrows either to the caller's
/// own requests, which is what "งานค้างของหมอ" means for the vet reading it. Always restricted to the
/// branches the caller works at.
/// </para>
/// </summary>
public sealed class ListLabRequestsHandler(
    ILabRequestRepository requests,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser)
{
    public async Task<CursorPage<LabRequestDto>> HandleAsync(
        ListLabRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        LabRequestStatus? status = null;

        if (query.Status is { Length: > 0 })
        {
            status = WireEnum.TryParse<LabRequestStatus>(query.Status, out var parsed)
                ? parsed
                : throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["status"] = [$"status ต้องเป็น {WireEnum.Names<LabRequestStatus>()}"],
                });
        }

        var requestedBy = query.RequestedBy is "me" ? currentUser.RequireStaffId() : query.RequestedBy;

        var page = await requests.ListAsync(
            new LabRequestFilter(status, query.PetId, requestedBy, query.BranchId, branchGuard.VisibleBranchIds),
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        // One patient's results are part of their file, and opening the file is on record. The
        // clinic-wide worklist is not: it is a queue of work, not a look at one patient.
        if (query.PetId is { Length: > 0 } petId)
        {
            await audit.RecordImmediatelyAsync(
                new AuditEvent(AuditActions.LabResultsViewed, AuditResources.Pet, petId, petId),
                cancellationToken);
        }

        return new CursorPage<LabRequestDto>([.. page.Data.Select(LabRequestDto.From)], page.NextCursor);
    }
}
