using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Articles.Manage;

/// <summary><c>POST /staff/articles/{id}/publish</c>. See <c>docs/contract-extensions.md § 26</c>.</summary>
/// <param name="ArticleId">From the path.</param>
/// <param name="IfMatchVersion">The article's <c>version</c>, when sent.</param>
public sealed record PublishArticleCommand(string ArticleId, uint? IfMatchVersion = null);

/// <summary>
/// Puts an article on the public site now. An article that was published before comes back under
/// its first date. One that is already public is <c>409 state_conflict</c>; a scheduled one is
/// brought forward to now. From the first publication on, the slug is fixed.
/// </summary>
public sealed class PublishArticleHandler(
    IArticleRepository articles,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    IClock clock)
{
    public async Task<StaffArticleDto> HandleAsync(PublishArticleCommand command, CancellationToken cancellationToken = default)
    {
        Domain.Articles.Article? article = null;
        var now = clock.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            article = await articles.FindAsync(command.ArticleId, token)
                ?? throw new NotFoundException("article", command.ArticleId);

            ConcurrencyGuard.Ensure(article, command.IfMatchVersion);

            var republished = article.FirstPublishedAt is not null;
            article.PublishNow(now);

            audit.Record(new AuditEvent(
                AuditActions.ArticlePublished,
                AuditResources.Article,
                article.Id,
                Details: new Dictionary<string, object?>
                {
                    ["slug"] = article.Slug,
                    ["title"] = article.Title,
                    ["published_at"] = article.PublishedAt,
                    ["republished"] = republished,
                }));
        }, cancellationToken);

        return StaffArticleDto.From(article!, now, storage);
    }
}
