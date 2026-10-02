namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// Where to send a file, and how. The client <c>PUT</c>s the bytes to <see cref="Url"/> with the
/// <see cref="Headers"/> given, before <see cref="ExpiresAt"/>.
/// </summary>
public sealed record UploadTicket(string Url, string Method, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt);

/// <summary>A file that has arrived, as the storage saw it.</summary>
public sealed record ReceivedFile(string Key, long SizeBytes);

/// <summary>A stored file opened for reading.</summary>
public sealed record StoredFile(Stream Content, string ContentType);

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
    string CreateDownloadUrl(string key, TimeSpan lifetime);

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
}
