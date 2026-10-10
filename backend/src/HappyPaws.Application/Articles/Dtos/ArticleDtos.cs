using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles.Dtos;

/// <summary>An article as a list shows it: everything but the body.</summary>
/// <param name="Id">Opaque id, <c>art_…</c>.</param>
/// <param name="Slug">The article's address.</param>
/// <param name="Title">Headline.</param>
/// <param name="Summary">One or two sentences.</param>
/// <param name="AuthorName">Byline.</param>
/// <param name="ReadingMinutes">Estimated reading time.</param>
/// <param name="CoverImagePath">Path under the web app's <c>/public</c>, from the seed; absent otherwise.</param>
/// <param name="CoverImageUrl">
/// A signed link to the cover staff uploaded, good for an hour (<c>docs/contract-extensions.md § 26</c>).
/// Absent when there is none. When both are present this one wins; in practice an upload clears the path.
/// </param>
/// <param name="PublishedAt">When it became public.</param>
public sealed record ArticleSummaryDto(
    string Id,
    string Slug,
    string Title,
    string Summary,
    string AuthorName,
    int ReadingMinutes,
    string? CoverImagePath,
    string? CoverImageUrl,
    DateTimeOffset PublishedAt)
{
    /// <summary>Only for an article that is published now: the cover link is minted here.</summary>
    public static ArticleSummaryDto From(Article article, IFileStorage storage) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.AuthorName,
        article.ReadingMinutes,
        article.CoverImagePath,
        ArticleCovers.LinkOf(article, storage, ArticleCovers.PublicLinkLifetime),
        article.PublishedAt!.Value);
}

/// <summary>One whole article.</summary>
/// <param name="Id">Opaque id, <c>art_…</c>.</param>
/// <param name="Slug">The article's address.</param>
/// <param name="Title">Headline.</param>
/// <param name="Summary">One or two sentences.</param>
/// <param name="Body">Plain text; a blank line separates paragraphs. Render as text, never as HTML.</param>
/// <param name="AuthorName">Byline.</param>
/// <param name="ReadingMinutes">Estimated reading time.</param>
/// <param name="CoverImagePath">Path under the web app's <c>/public</c>, from the seed; absent otherwise.</param>
/// <param name="CoverImageUrl">A signed link to the uploaded cover, good for an hour; absent when there is none.</param>
/// <param name="PublishedAt">When it became public.</param>
public sealed record ArticleDto(
    string Id,
    string Slug,
    string Title,
    string Summary,
    string Body,
    string AuthorName,
    int ReadingMinutes,
    string? CoverImagePath,
    string? CoverImageUrl,
    DateTimeOffset PublishedAt)
{
    /// <summary>Only for an article that is published now: the cover link is minted here.</summary>
    public static ArticleDto From(Article article, IFileStorage storage) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.Body,
        article.AuthorName,
        article.ReadingMinutes,
        article.CoverImagePath,
        ArticleCovers.LinkOf(article, storage, ArticleCovers.PublicLinkLifetime),
        article.PublishedAt!.Value);
}
