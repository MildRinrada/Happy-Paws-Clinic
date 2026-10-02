using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles.Dtos;

/// <summary>An article as a list shows it: everything but the body.</summary>
/// <param name="Id">Opaque id, <c>art_…</c>.</param>
/// <param name="Slug">The article's address.</param>
/// <param name="Title">Headline.</param>
/// <param name="Summary">One or two sentences.</param>
/// <param name="AuthorName">Byline.</param>
/// <param name="ReadingMinutes">Estimated reading time.</param>
/// <param name="CoverImagePath">Path under the web app's <c>/public</c>; absent until a picture is supplied.</param>
/// <param name="PublishedAt">When it became public.</param>
public sealed record ArticleSummaryDto(
    string Id,
    string Slug,
    string Title,
    string Summary,
    string AuthorName,
    int ReadingMinutes,
    string? CoverImagePath,
    DateTimeOffset PublishedAt)
{
    public static ArticleSummaryDto From(Article article) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.AuthorName,
        article.ReadingMinutes,
        article.CoverImagePath,
        article.PublishedAt);
}

/// <summary>One whole article.</summary>
/// <param name="Id">Opaque id, <c>art_…</c>.</param>
/// <param name="Slug">The article's address.</param>
/// <param name="Title">Headline.</param>
/// <param name="Summary">One or two sentences.</param>
/// <param name="Body">Plain text; a blank line separates paragraphs. Render as text, never as HTML.</param>
/// <param name="AuthorName">Byline.</param>
/// <param name="ReadingMinutes">Estimated reading time.</param>
/// <param name="CoverImagePath">Path under the web app's <c>/public</c>; absent until a picture is supplied.</param>
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
    DateTimeOffset PublishedAt)
{
    public static ArticleDto From(Article article) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.Body,
        article.AuthorName,
        article.ReadingMinutes,
        article.CoverImagePath,
        article.PublishedAt);
}
