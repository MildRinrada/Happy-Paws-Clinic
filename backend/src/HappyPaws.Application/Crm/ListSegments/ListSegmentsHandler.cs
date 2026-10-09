using FluentValidation;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;

namespace HappyPaws.Application.Crm.ListSegments;

/// <summary>
/// <c>GET /staff/segments?branch=&amp;limit=&amp;cursor=</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
public sealed record ListSegmentsQuery(string? BranchId = null, int Limit = CursorPaging.DefaultLimit, string? Cursor = null);

public sealed class ListSegmentsValidator : AbstractValidator<ListSegmentsQuery>
{
    public ListSegmentsValidator()
    {
        RuleFor(query => query.BranchId).MaximumLength(40);
    }
}

/// <summary>
/// The saved customer groups, newest first, with the counts taken when each was saved
/// (<c>counted_at</c> says when). Counting every group again on every page would run the segment
/// query once per row; <c>GET /staff/segments/{id}</c> counts one group again.
/// <para>
/// Branch-bound staff see only groups limited to one of their branches. A group over every branch
/// is not theirs to see, for the reason they cannot make one: its count describes customers of
/// branches they cannot see.
/// </para>
/// </summary>
public sealed class ListSegmentsHandler(ISegmentRepository segments, BranchGuard branchGuard, RequestValidator validator)
{
    public async Task<CursorPage<SegmentDto>> HandleAsync(ListSegmentsQuery query, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(query, cancellationToken);

        var branchId = query.BranchId is { Length: > 0 } requested ? requested : null;

        if (branchId is not null)
        {
            branchGuard.EnsureCanAccess(branchId);
        }

        var page = await segments.ListAsync(
            branchId,
            branchGuard.VisibleBranchIds,
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        return new CursorPage<SegmentDto>([.. page.Data.Select(SegmentDto.From)], page.NextCursor);
    }
}
