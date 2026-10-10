using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.Articles.Manage;

/// <summary>
/// Swaps in an article's new cover when its file arrives at the storage's upload URL
/// (<c>docs/contract-extensions.md § 26</c>). The job an S3 event notification would do for a
/// cloud bucket, in the same place the pet documents get theirs (<c>StoredFilesHandler</c>).
/// <para>
/// Only the key the article is waiting for counts. A file for a key the article no longer waits
/// for, because a later upload was asked for or the article was deleted, is kept by the storage
/// but never shown.
/// </para>
/// </summary>
public sealed class ArticleCoverReceiver(IArticleRepository articles, IUnitOfWork unitOfWork)
{
    /// <summary>True when the file was an article cover this call put in place.</summary>
    public async Task<bool> ReceivedAsync(ReceivedFile file, CancellationToken cancellationToken = default)
    {
        if (!file.Key.StartsWith(ArticleCovers.KeyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var placed = false;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            placed = await articles.FindByPendingCoverKeyAsync(file.Key, token) is { } article
                && article.CoverArrived(file.Key);
        }, cancellationToken);

        return placed;
    }
}
