using HappyPaws.Application.Articles.Manage;
using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.Patients.Documents;

/// <summary>
/// The two storage routes of the local adapter: receiving an upload, and serving a download.
/// <para>
/// The signed token is the whole of the authorisation, as it is for an S3 URL. Receiving a file
/// also marks the document it belongs to as uploaded, which is the job an S3 event notification
/// would do for a cloud bucket. An article's new cover (<c>docs/contract-extensions.md § 26</c>)
/// is swapped in the same way, by <see cref="ArticleCoverReceiver"/>.
/// </para>
/// </summary>
public sealed class StoredFilesHandler(
    IFileStorage storage,
    IPetDocumentRepository documents,
    IUnitOfWork unitOfWork,
    IClock clock,
    ArticleCoverReceiver articleCovers)
{
    /// <summary>True when the file was accepted.</summary>
    public async Task<bool> ReceiveAsync(
        string token,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var received = await storage.ReceiveAsync(token, contentType, content, cancellationToken);
        if (received is null)
        {
            return false;
        }

        var document = await documents.FindByStorageKeyAsync(received.Key, cancellationToken);
        if (document is not null)
        {
            document.MarkUploaded(received.SizeBytes, clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        await articleCovers.ReceivedAsync(received, cancellationToken);

        return true;
    }

    public Task<StoredFile?> OpenAsync(string token, CancellationToken cancellationToken = default) =>
        storage.OpenAsync(token, cancellationToken);
}
