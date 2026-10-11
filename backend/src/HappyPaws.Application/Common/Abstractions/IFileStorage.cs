namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// Where to send a file, and how. The client <c>PUT</c>s the bytes to <see cref="Url"/> with the
/// <see cref="Headers"/> given, before <see cref="ExpiresAt"/>.
/// </summary>
public sealed record UploadTicket(string Url, string Method, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt);

/// <summary>A file that has arrived, as the storage saw it.</summary>
public sealed record ReceivedFile(string Key, long SizeBytes);

/// <summary>A stored file opened for reading.</summary>
public sealed record StoredFile(Stream Content, string ContentType)
{
    /// <summary>The name a browser should save it under, when the file was stored with one.</summary>
    public string? FileName { get; init; }
}

/// <summary>
/// Object storage behind pre-signed URLs — "อัปโหลดผ่าน pre-signed URL".
/// <para>
/// The API never carries file bytes through a use case. A use case asks for an upload URL, the
/// client sends the file straight there, and a download is a second short-lived URL. That is how
/// S3 and its kin work, and it is the shape this port keeps, so moving to one of them changes the
/// adapter and nothing above it.
/// </para>
/// <para>
/// The adapter in this repository keeps files on local disk and serves its signed URLs from the
/// API itself (<c>/storage/uploads/{token}</c>, <c>/storage/files/{token}</c>). Those two routes
/// are the storage, not part of the clinic's API; see <c>docs/contract-extensions.md § 12</c>.
/// </para>
/// </summary>
public interface IFileStorage
{
    /// <summary>A URL that accepts one file of this type and at most this size at this key.</summary>
    UploadTicket CreateUpload(string key, string contentType, long maxBytes, TimeSpan lifetime);

    /// <summary>A URL that serves the file at this key until it expires.</summary>
    /// <param name="key">The file.</param>
    /// <param name="lifetime">How long the URL works.</param>
    /// <param name="saveAs">
    /// When given, the file is served as a download under this name (<c>Content-Disposition:
    /// attachment</c>) instead of in the browser. A lab's PDF is (<c>docs/contract-extensions.md § 54</c>).
    /// </param>
    string CreateDownloadUrl(string key, TimeSpan lifetime, string? saveAs = null);

    /// <summary>
    /// Stores a file sent to an upload URL. Returns null when the token is not a valid, unexpired
    /// upload token for this content type, or the body is larger than it allows.
    /// Only the local adapter serves uploads itself; a cloud adapter never receives this call.
    /// </summary>
    Task<ReceivedFile?> ReceiveAsync(
        string token,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>Opens the file a download token points to, or null when the token or the file is not good.</summary>
    Task<StoredFile?> OpenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a file has arrived at this key. A cloud adapter answers it with a HEAD on the
    /// object. Used where a use case must not point at a file that was never sent, such as a staff
    /// member's uploaded photo (<c>docs/contract-extensions.md § 32</c>).
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a file the server produced itself, such as a report export, at a key a use case made.
    /// Returns its size. The one write that does not come through an upload URL, because there is
    /// no client to send it: the bytes are already here.
    /// </summary>
    /// <param name="key">Where to keep it.</param>
    /// <param name="contentType">What a download serves it as.</param>
    /// <param name="content">The bytes.</param>
    /// <param name="fileName">The name a download should be saved under.</param>
    /// <param name="cancellationToken">Aborts the write.</param>
    Task<long> SaveAsync(
        string key,
        string contentType,
        Stream content,
        string? fileName = null,
        CancellationToken cancellationToken = default);
}
