using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Articles.Manage;

/// <summary><c>POST /staff/articles/{id}/unpublish</c>. See <c>docs/contract-extensions.md § 26</c>.</summary>
/// <param name="ArticleId">From the path.</param>
/// <param name="IfMatchVersion">The article's <c>version</c>, when sent.</param>
public sealed record UnpublishArticleCommand(string ArticleId, uint? IfMatchVersion = null);

/// <summary>
/// Takes an article off the public site, or cancels its scheduled time. It is a draft again: the
/// public list drops it and its page answers <c>404</c> like one that never existed. The slug stays
/// fixed if it was ever published. A draft is <c>409 state_conflict</c>.
/// </summary>
public sealed class UnpublishArticleHandler(
    IArticleRepository articles,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    IClock clock)
{
    public async Task<StaffArticleDto> HandleAsync(UnpublishArticleCommand command, CancellationToken cancellationToken = default)
    {
        Domain.Articles.Article? article = null;
        var now = clock.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            article = await articles.FindAsync(command.ArticleId, token)
                ?? throw new NotFoundException("article", command.ArticleId);

            ConcurrencyGuard.Ensure(article, command.IfMatchVersion);

            var was = WireEnum.Of(article.StatusAt(now));
            article.Unpublish();

            audit.Record(new AuditEvent(
                AuditActions.ArticleUnpublished,
                AuditResources.Article,
                article.Id,
                Details: new Dictionary<string, object?>
                {
                    ["slug"] = article.Slug,
                    ["title"] = article.Title,
                    ["was"] = was,
                }));
        }, cancellationToken);

        return StaffArticleDto.From(article!, now, storage);
    }
}
