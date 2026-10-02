using HappyPaws.Application.Articles.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Articles.GetArticle;

/// <summary><c>GET /articles/{slug}</c>, a contract extension (<c>docs/contract-extensions.md § 9</c>).</summary>
/// <param name="Slug">The article's address.</param>
public sealed record GetArticleQuery(string Slug);

/// <summary>
/// One published article. A scheduled one answers <c>404</c> exactly like a missing one, so the
/// public cannot find out what is coming before it is meant to be read.
/// </summary>
public sealed class GetArticleHandler(IArticleRepository articles, IClock clock)
{
    public async Task<ArticleDto> HandleAsync(GetArticleQuery query, CancellationToken cancellationToken = default)
    {
        var article = await articles.FindPublishedBySlugAsync(query.Slug, clock.UtcNow, cancellationToken)
            ?? throw new NotFoundException("article", query.Slug);

        return ArticleDto.From(article);
    }
}
