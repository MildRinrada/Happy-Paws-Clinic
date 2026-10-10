using HappyPaws.Application.Articles;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Articles;
using HappyPaws.Infrastructure.Persistence.Repositories.Crm;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Articles;

/// <summary>EF Core implementation of the article port.</summary>
internal sealed class ArticleRepository(HappyPawsDbContext db) : IArticleRepository
{
    public void Add(Article article) => db.Set<Article>().Add(article);

    public async Task<IReadOnlySet<string>> ExistingSlugsAsync(
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken = default)
    {
        // Deleted articles included: a sample the clinic deleted is not seeded back.
        var taken = await db.Set<Article>().AsNoTracking()
            .Where(article => slugs.Contains(article.Slug))
            .Select(article => article.Slug)
            .ToListAsync(cancellationToken);

        return taken.ToHashSet(StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<Article>> ListPublishedAsync(
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken = default) =>
        await Public(now)
            // The id is time-ordered, so it breaks a tie between two articles published together.
            .OrderByDescending(article => article.PublishedAt).ThenByDescending(article => article.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<Article?> FindPublishedBySlugAsync(
        string slug,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Public(now).FirstOrDefaultAsync(article => article.Slug == slug, cancellationToken);

    public Task<Article?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        db.Set<Article>().FirstOrDefaultAsync(article => article.Id == id && article.DeletedAt == null, cancellationToken);

    public async Task<SlugHolder?> FindSlugHolderAsync(string slug, CancellationToken cancellationToken = default) =>
        await db.Set<Article>().AsNoTracking()
            .Where(article => article.Slug == slug)
            .Select(article => new SlugHolder(article.Id, article.DeletedAt != null))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Article?> FindByPendingCoverKeyAsync(string key, CancellationToken cancellationToken = default) =>
        db.Set<Article>().FirstOrDefaultAsync(article => article.PendingCoverKey == key, cancellationToken);

    public async Task<CursorPage<Article>> ListForStaffAsync(
        StaffArticleFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var articles = db.Set<Article>().AsNoTracking().Where(article => article.DeletedAt == null);
        var now = filter.Now;

        // The same rules as Article.StatusAt, so a filtered row shows the status it was found by.
        articles = filter.Status switch
        {
            ArticleStatus.Draft => articles.Where(article => article.PublishedAt == null),
            ArticleStatus.Scheduled => articles.Where(article => article.PublishedAt > now),
            ArticleStatus.Published => articles.Where(article => article.PublishedAt <= now),
            _ => articles,
        };

        if (filter.Search is { Length: > 0 } search)
        {
            var pattern = $"%{search.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
            articles = articles.Where(article => EF.Functions.ILike(article.Title, pattern) || EF.Functions.ILike(article.Slug, pattern));
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            articles = articles.Where(article =>
                article.CreatedAt < sortKey || (article.CreatedAt == sortKey && string.Compare(article.Id, id) < 0));
        }

        var rows = await articles
            .OrderByDescending(article => article.CreatedAt).ThenByDescending(article => article.Id)
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        return NewestFirst.Page(rows, page, article => article.CreatedAt, article => article.Id);
    }

    /// <summary>
    /// What the public may see: published by now and not deleted. A draft has no publication time,
    /// so <c>published_at &lt;= now</c> alone already leaves it out; the deleted check is the
    /// second lock on the same door.
    /// </summary>
    private IQueryable<Article> Public(DateTimeOffset now) =>
        db.Set<Article>().AsNoTracking()
            .Where(article => article.DeletedAt == null && article.PublishedAt != null && article.PublishedAt <= now);
}
