using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Articles.Manage;

/// <summary>
/// <c>GET /staff/articles/{id}</c>: one article for the editor, draft or not, with a preview link
/// to its cover. A deleted article is <c>404</c>. See <c>docs/contract-extensions.md § 26</c>.
/// </summary>
public sealed class GetStaffArticleHandler(IArticleRepository articles, IFileStorage storage, IClock clock)
{
    public async Task<StaffArticleDto> HandleAsync(string articleId, CancellationToken cancellationToken = default)
    {
        var article = await articles.FindAsync(articleId, cancellationToken)
            ?? throw new NotFoundException("article", articleId);

        return StaffArticleDto.From(article, clock.UtcNow, storage);
    }
}
