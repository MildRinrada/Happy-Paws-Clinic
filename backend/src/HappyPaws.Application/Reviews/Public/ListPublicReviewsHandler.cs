using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Public;

/// <summary><c>GET /reviews?limit=&amp;cursor=</c>.</summary>
/// <param name="Limit">Page size, 1 to 100.</param>
/// <param name="Cursor">From the previous page.</param>
public sealed record ListPublicReviewsQuery(int Limit = CursorPaging.DefaultLimit, string? Cursor = null);

/// <summary>
/// Published reviews, newest written first, with the summary over every published review. Public.
/// Not in the API reference: see <c>docs/contract-extensions.md § 36</c>.
/// <para>
/// Only published reviews are read, for the list and for the figures. A pending review has not been
/// read by staff and a rejected one was taken down for a listed reason; neither counts. Each review
/// shows the first name or "ลูกค้า", the pet's species, the score, the words and the clinic's reply,
/// and never a surname, phone, email or the pet's name.
/// </para>
/// </summary>
public sealed class ListPublicReviewsHandler(IReviewRepository reviews)
{
    public async Task<PublicReviewPageDto> HandleAsync(ListPublicReviewsQuery query, CancellationToken cancellationToken = default)
    {
        var page = await reviews.ListPublishedAsync(new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);
        var counts = await reviews.PublishedCountsByRatingAsync(cancellationToken);

        return new PublicReviewPageDto(
            [.. page.Data.Select(PublicReviewDto.From)],
            page.NextCursor,
            ReviewSummaryDto.From(ReviewSummary.From(counts)));
    }
}
