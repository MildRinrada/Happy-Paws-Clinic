using FluentValidation;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles.Manage;

/// <summary>
/// <c>GET /staff/articles?status=&amp;q=&amp;limit=&amp;cursor=</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 26</c>.
/// </summary>
/// <param name="Status"><c>draft</c>, <c>scheduled</c> or <c>published</c>; every one when absent.</param>
/// <param name="Search">Part of the title or the slug.</param>
/// <param name="Limit">Page size, 1 to 100.</param>
/// <param name="Cursor">From the previous page.</param>
public sealed record ListStaffArticlesQuery(
    string? Status = null,
    string? Search = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

public sealed class ListStaffArticlesValidator : AbstractValidator<ListStaffArticlesQuery>
{
    public ListStaffArticlesValidator()
    {
        // Ignoring an unknown status would answer with every article and look like a filtered list.
        RuleFor(query => query.Status)
            .Must(status => WireEnum.TryParse<ArticleStatus>(status, out _))
            .WithMessage($"status ต้องเป็น {WireEnum.Names<ArticleStatus>()}")
            .When(query => !string.IsNullOrEmpty(query.Status));

        RuleFor(query => query.Search).MaximumLength(120);
    }
}

/// <summary>
/// Every article that is not deleted, drafts included, newest written first. The status is worked
/// out against the same moment the filter uses, so a row always shows the status it was found by.
/// </summary>
public sealed class ListStaffArticlesHandler(IArticleRepository articles, RequestValidator validator, IClock clock)
{
    public async Task<CursorPage<StaffArticleSummaryDto>> HandleAsync(
        ListStaffArticlesQuery query,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(query, cancellationToken);

        var now = clock.UtcNow;
        ArticleStatus? status = WireEnum.TryParse<ArticleStatus>(query.Status, out var parsed) ? parsed : null;
        var filter = new StaffArticleFilter(now, status, query.Search?.Trim() is { Length: > 0 } search ? search : null);

        var page = await articles.ListForStaffAsync(filter, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        return new CursorPage<StaffArticleSummaryDto>(
            [.. page.Data.Select(article => StaffArticleSummaryDto.From(article, now))],
            page.NextCursor);
    }
}
