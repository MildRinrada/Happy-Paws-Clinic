using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Crm.GetSegment;

/// <summary>
/// <c>GET /staff/segments/{id}</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>.
/// <para>
/// One saved group, counted again now with the same SQL the broadcast sends with, so the numbers on
/// screen are the numbers a broadcast would reach. The counts stored on the group are not changed:
/// a read writes nothing. The same branch rule as the list: a group over every branch, or another
/// branch's group, is <c>403</c> for staff bound to branches.
/// </para>
/// </summary>
public sealed class GetSegmentHandler(
    ISegmentRepository segments,
    ISegmentQuery query,
    BranchGuard branchGuard,
    IClock clock,
    ClinicDays clinicDays)
{
    public async Task<SegmentDto> HandleAsync(string segmentId, CancellationToken cancellationToken = default)
    {
        var segment = await segments.FindByIdAsync(segmentId, cancellationToken)
            ?? throw new NotFoundException("segment", segmentId);

        if (segment.BranchId is null)
        {
            if (branchGuard.VisibleBranchIds is not null)
            {
                throw new ForbiddenException("a branch-bound staff member cannot see a segment over every branch");
            }
        }
        else
        {
            branchGuard.EnsureCanAccess(segment.BranchId);
        }

        var counts = await query.CountAsync(segment.Criteria, clinicDays.Today, cancellationToken);

        return SegmentDto.From(segment, counts, clock.UtcNow);
    }
}
