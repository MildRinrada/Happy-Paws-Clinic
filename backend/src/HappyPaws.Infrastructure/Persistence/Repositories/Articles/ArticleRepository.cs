using HappyPaws.Application.Articles;
using HappyPaws.Domain.Articles;
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
        await db.Set<Article>().AsNoTracking()
            .Where(article => article.PublishedAt <= now)
            // The id is time-ordered, so it breaks a tie between two articles published together.
            .OrderByDescending(article => article.PublishedAt).ThenByDescending(article => article.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<Article?> FindPublishedBySlugAsync(
        string slug,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        db.Set<Article>().AsNoTracking()
            .FirstOrDefaultAsync(article => article.Slug == slug && article.PublishedAt <= now, cancellationToken);
}
