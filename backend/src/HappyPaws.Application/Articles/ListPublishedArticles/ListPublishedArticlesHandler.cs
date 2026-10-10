using HappyPaws.Application.Articles.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Articles.ListPublishedArticles;

/// <summary><c>GET /articles</c>, a contract extension (<c>docs/contract-extensions.md § 9</c>).</summary>
/// <param name="Limit">How many, newest first. 1 to 50; 10 when absent.</param>
public sealed record ListPublishedArticlesQuery(int? Limit = null);

/// <summary>
/// The public list of articles, newest first.
/// <para>
/// A plain limit rather than a cursor. The list is short, read mostly by the home page for its
/// three newest, and a clinic publishes a handful a month; a cursor can be added without breaking
/// anyone if that ever changes. Scheduled articles stay hidden until their time comes.
/// </para>
/// </summary>
public sealed class ListPublishedArticlesHandler(IArticleRepository articles, IClock clock, IFileStorage storage)
{
    public const int DefaultLimit = 10;
    public const int MaxLimit = 50;

    public async Task<IReadOnlyList<ArticleSummaryDto>> HandleAsync(
        ListPublishedArticlesQuery query,
        CancellationToken cancellationToken = default)
    {
        var limit = query.Limit ?? DefaultLimit;

        if (limit is < 1 or > MaxLimit)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["limit"] = [$"limit ต้องอยู่ระหว่าง 1 ถึง {MaxLimit}"] });
        }

        var published = await articles.ListPublishedAsync(clock.UtcNow, limit, cancellationToken);

        return [.. published.Select(article => ArticleSummaryDto.From(article, storage))];
    }
}
