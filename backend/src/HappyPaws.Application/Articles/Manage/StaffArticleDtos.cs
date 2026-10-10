using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles.Manage;

/// <summary>
/// An article as staff see it in the list: drafts included, everything but the body
/// (<c>docs/contract-extensions.md § 26</c>).
/// </summary>
/// <param name="Id">Opaque id, <c>art_…</c>.</param>
/// <param name="Slug">The article's address.</param>
/// <param name="Title">Headline.</param>
/// <param name="Summary">One or two sentences.</param>
/// <param name="AuthorName">Byline.</param>
/// <param name="ReadingMinutes">Estimated reading time.</param>
/// <param name="Status"><c>draft</c>, <c>scheduled</c> or <c>published</c>, worked out now.</param>
/// <param name="PublishedAt">When it went or goes public; absent for a draft.</param>
/// <param name="FirstPublishedAt">The first time it was published; absent if it never was.</param>
/// <param name="SlugLocked">True once it has ever been published: the slug can no longer change.</param>
/// <param name="HasCover">True when there is a cover, uploaded or seeded.</param>
/// <param name="CreatedAt">When the draft was written.</param>
/// <param name="UpdatedAt">The last change.</param>
/// <param name="Version">Echo as <c>If-Match</c>.</param>
public sealed record StaffArticleSummaryDto(
    string Id,
    string Slug,
    string Title,
    string Summary,
    string AuthorName,
    int ReadingMinutes,
    string Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? FirstPublishedAt,
    bool SlugLocked,
    bool HasCover,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    uint Version)
{
    public static StaffArticleSummaryDto From(Article article, DateTimeOffset now) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.AuthorName,
        article.ReadingMinutes,
        WireEnum.Of(article.StatusAt(now)),
        article.PublishedAt,
        article.FirstPublishedAt,
        article.IsSlugLocked,
        article.CoverImageKey is not null || article.CoverImagePath is not null,
        article.CreatedAt,
        article.UpdatedAt,
        article.Version);
}

/// <summary>One article as the editor opens it (<c>docs/contract-extensions.md § 26</c>).</summary>
/// <param name="Id">Opaque id, <c>art_…</c>.</param>
/// <param name="Slug">The article's address.</param>
/// <param name="Title">Headline.</param>
/// <param name="Summary">One or two sentences.</param>
/// <param name="Body">Plain text as stored, after cleaning.</param>
/// <param name="AuthorName">Byline.</param>
/// <param name="ReadingMinutes">Estimated reading time.</param>
/// <param name="Status"><c>draft</c>, <c>scheduled</c> or <c>published</c>, worked out now.</param>
/// <param name="PublishedAt">When it went or goes public; absent for a draft.</param>
/// <param name="FirstPublishedAt">The first time it was published; absent if it never was.</param>
/// <param name="SlugLocked">True once it has ever been published: the slug can no longer change.</param>
/// <param name="CoverImagePath">A seeded picture under the web app's <c>/public</c>.</param>
/// <param name="CoverImageUrl">A signed link to the uploaded cover, good for half an hour, for the preview.</param>
/// <param name="CoverPending">True while an upload URL is out and its file has not arrived.</param>
/// <param name="CreatedAt">When the draft was written.</param>
/// <param name="UpdatedAt">The last change.</param>
/// <param name="Version">Echo as <c>If-Match</c>.</param>
public sealed record StaffArticleDto(
    string Id,
    string Slug,
    string Title,
    string Summary,
    string Body,
    string AuthorName,
    int ReadingMinutes,
    string Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? FirstPublishedAt,
    bool SlugLocked,
    string? CoverImagePath,
    string? CoverImageUrl,
    bool CoverPending,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    uint Version)
{
    public static StaffArticleDto From(Article article, DateTimeOffset now, IFileStorage storage) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.Body,
        article.AuthorName,
        article.ReadingMinutes,
        WireEnum.Of(article.StatusAt(now)),
        article.PublishedAt,
        article.FirstPublishedAt,
        article.IsSlugLocked,
        article.CoverImagePath,
        ArticleCovers.LinkOf(article, storage, ArticleCovers.StaffLinkLifetime),
        article.PendingCoverKey is not null,
        article.CreatedAt,
        article.UpdatedAt,
        article.Version);
}

/// <summary>The article as it stands, and where to <c>PUT</c> the cover's bytes.</summary>
/// <param name="Article">The article, with <c>cover_pending</c> now true.</param>
/// <param name="Upload">The signed URL, method and headers for the file.</param>
public sealed record ArticleCoverUploadDto(StaffArticleDto Article, UploadTicketDto Upload);
