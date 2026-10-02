using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles;

/// <summary>Persistence port for health articles.</summary>
public interface IArticleRepository
{
    void Add(Article article);

    /// <summary>Which of these slugs are already taken. For seeding without duplicates.</summary>
    Task<IReadOnlySet<string>> ExistingSlugsAsync(
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken = default);

    /// <summary>Published by <paramref name="now"/>, newest first.</summary>
    Task<IReadOnlyList<Article>> ListPublishedAsync(
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>The article at this slug, or null if there is none or it is not published yet.</summary>
    Task<Article?> FindPublishedBySlugAsync(
        string slug,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
