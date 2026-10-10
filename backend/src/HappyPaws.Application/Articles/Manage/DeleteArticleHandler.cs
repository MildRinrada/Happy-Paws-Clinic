using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Articles.Manage;

/// <summary><c>DELETE /staff/articles/{id}</c>. See <c>docs/contract-extensions.md § 26</c>.</summary>
/// <param name="ArticleId">From the path.</param>
/// <param name="IfMatchVersion">The article's <c>version</c>, when sent.</param>
public sealed record DeleteArticleCommand(string ArticleId, uint? IfMatchVersion = null);

/// <summary>
/// Deletes an article, in any state. A published one leaves the public site in the same step.
/// <para>
/// The row stays, marked deleted, and leaves every staff list and read (<c>404</c> from then on,
/// a second delete included). Its slug stays taken, so the address never opens a different article
/// and <c>seed-articles</c> never brings a deleted sample back. There is no undelete; writing it
/// again under a new slug is the way back.
/// </para>
/// </summary>
public sealed class DeleteArticleHandler(
    IArticleRepository articles,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task HandleAsync(DeleteArticleCommand command, CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var article = await articles.FindAsync(command.ArticleId, token)
                ?? throw new NotFoundException("article", command.ArticleId);

            ConcurrencyGuard.Ensure(article, command.IfMatchVersion);

            var was = WireEnum.Of(article.StatusAt(now));
            article.Delete(staffId, now);

            audit.Record(new AuditEvent(
                AuditActions.ArticleDeleted,
                AuditResources.Article,
                article.Id,
                Details: new Dictionary<string, object?>
                {
                    ["slug"] = article.Slug,
                    ["title"] = article.Title,
                    ["was"] = was,
                }));
        }, cancellationToken);
    }
}
