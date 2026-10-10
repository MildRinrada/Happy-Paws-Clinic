using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Staff;

/// <summary><c>GET /staff/reviews?status=&amp;limit=&amp;cursor=</c>.</summary>
/// <param name="Status"><c>pending</c>, <c>published</c> or <c>rejected</c>; every one when absent.</param>
/// <param name="Limit">Page size, 1 to 100.</param>
/// <param name="Cursor">From the previous page.</param>
public sealed record ListStaffReviewsQuery(string? Status = null, int Limit = CursorPaging.DefaultLimit, string? Cursor = null);

/// <summary>
/// The moderation list (<c>content:manage</c>), newest written first. Not in the API reference: see
/// <c>docs/contract-extensions.md § 36</c>. Staff see who wrote each review and about which pet,
/// which the public page never shows. An unknown status is <c>422</c> rather than every review,
/// so a typo cannot pass for a filtered list.
/// </summary>
public sealed class ListStaffReviewsHandler(IReviewRepository reviews)
{
    public async Task<CursorPage<StaffReviewDto>> HandleAsync(ListStaffReviewsQuery query, CancellationToken cancellationToken = default)
    {
        ReviewStatus? status = null;

        if (!string.IsNullOrEmpty(query.Status))
        {
            if (!WireEnum.TryParse<ReviewStatus>(query.Status, out var parsed))
            {
                throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["status"] = [$"status ต้องเป็น {WireEnum.Names<ReviewStatus>()}"],
                });
            }

            status = parsed;
        }

        var page = await reviews.ListForStaffAsync(status, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        return new CursorPage<StaffReviewDto>([.. page.Data.Select(StaffReviewDto.From)], page.NextCursor);
    }
}
