using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory;

/// <summary>Persistence port for lab requests. Every load includes the results.</summary>
public interface ILabRequestRepository
{
    void Add(LabRequest request);

    /// <summary>Tracked, with results, for a write.</summary>
    Task<LabRequest?> FindTrackedByIdAsync(string labRequestId, CancellationToken cancellationToken = default);

    /// <summary>Newest first.</summary>
    Task<CursorPage<LabRequest>> ListAsync(
        LabRequestFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// What the lab worklist may be narrowed by. <c>BranchIds</c> is the caller's visible branches, or
/// null when unrestricted, and is always applied.
/// </summary>
public sealed record LabRequestFilter(
    LabRequestStatus? Status = null,
    string? PetId = null,
    string? RequestedByStaffId = null,
    string? BranchId = null,
    IReadOnlyCollection<string>? BranchIds = null);
