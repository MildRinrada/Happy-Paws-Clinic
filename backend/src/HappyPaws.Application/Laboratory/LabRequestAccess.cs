using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory;

/// <summary>
/// Loads a lab request for a write, within the caller's branches.
/// <para>
/// A request at a branch the caller does not work at is answered <c>404</c>, the same as one that
/// does not exist. The worklist already hides it, and a <c>403</c> would confirm the id is real.
/// </para>
/// </summary>
public sealed class LabRequestAccess(ILabRequestRepository requests, BranchGuard branchGuard)
{
    public async Task<LabRequest> ForStaffAsync(string labRequestId, CancellationToken cancellationToken = default)
    {
        var request = await requests.FindTrackedByIdAsync(labRequestId, cancellationToken);

        if (request is null || !branchGuard.CanAccess(request.BranchId))
        {
            throw new NotFoundException("lab_request", labRequestId);
        }

        return request;
    }
}
