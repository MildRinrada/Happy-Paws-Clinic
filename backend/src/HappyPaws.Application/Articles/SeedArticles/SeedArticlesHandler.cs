using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles.SeedArticles;

/// <summary>What <c>seed-articles</c> did.</summary>
/// <param name="Created">Slugs published by this run.</param>
/// <param name="Skipped">Slugs that already existed and were left exactly as they were.</param>
public sealed record SeedArticlesResult(IReadOnlyList<string> Created, IReadOnlyList<string> Skipped);

/// <summary>
/// Publishes the sample articles in <see cref="SampleArticles"/>. Run by the operator's
/// <c>seed-articles</c> command, never by a request.
/// <para>
/// Safe to run again: an article whose slug already exists is skipped, not overwritten, so a clinic
/// that has edited a sample in the database does not lose the edit to a second seed. A sample staff
/// deleted keeps its slug (<c>docs/contract-extensions.md § 26</c>), so it is skipped too and not
/// brought back.
/// </para>
/// </summary>
public sealed class SeedArticlesHandler(IArticleRepository articles, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<SeedArticlesResult> HandleAsync(CancellationToken cancellationToken = default)
    {
        var created = new List<string>();
        var skipped = new List<string>();

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // The transaction may be retried, so the result lists start again with it.
            created.Clear();
            skipped.Clear();

            var samples = SampleArticles.All;
            var taken = await articles.ExistingSlugsAsync([.. samples.Select(sample => sample.Slug)], ct);
            var now = clock.UtcNow;

            foreach (var sample in samples)
            {
                if (taken.Contains(sample.Slug))
                {
                    skipped.Add(sample.Slug);
                    continue;
                }

                articles.Add(Article.Publish(
                    sample.Slug,
                    sample.Title,
                    sample.Summary,
                    sample.Body,
                    sample.AuthorName,
                    sample.ReadingMinutes,
                    now.AddDays(-sample.DaysAgo)));

                created.Add(sample.Slug);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return new SeedArticlesResult(created, skipped);
    }
}
