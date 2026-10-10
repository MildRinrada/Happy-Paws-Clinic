using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles;

/// <summary>What the staff list of articles is narrowed to.</summary>
/// <param name="Now">The moment "scheduled" and "published" are judged against.</param>
/// <param name="Status">One status, or every one when null.</param>
/// <param name="Search">Part of the title or the slug.</param>
public sealed record StaffArticleFilter(DateTimeOffset Now, ArticleStatus? Status, string? Search);

/// <summary>Who holds a slug, for the "already taken" answer.</summary>
/// <param name="ArticleId">The article holding it.</param>
/// <param name="Deleted">True when that article was deleted: deleted articles keep their slug.</param>
public sealed record SlugHolder(string ArticleId, bool Deleted);

/// <summary>Persistence port for health articles.</summary>
public interface IArticleRepository
{
    void Add(Article article);

    /// <summary>Which of these slugs are already taken, deleted articles included. For seeding without duplicates.</summary>
    Task<IReadOnlySet<string>> ExistingSlugsAsync(
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken = default);

    /// <summary>Published by <paramref name="now"/> and not deleted, newest first.</summary>
    Task<IReadOnlyList<Article>> ListPublishedAsync(
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>The article at this slug, or null if there is none, it is not published yet, or it was deleted.</summary>
    Task<Article?> FindPublishedBySlugAsync(
        string slug,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>One article for staff, tracked for change. Null when absent or deleted.</summary>
    Task<Article?> FindAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Who holds this slug, deleted articles included, or null when it is free.</summary>
    Task<SlugHolder?> FindSlugHolderAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>The article an upload URL for this storage key was issued to, tracked for change.</summary>
    Task<Article?> FindByPendingCoverKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Every article that is not deleted, drafts included, newest written first.</summary>
    Task<CursorPage<Article>> ListForStaffAsync(
        StaffArticleFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}
