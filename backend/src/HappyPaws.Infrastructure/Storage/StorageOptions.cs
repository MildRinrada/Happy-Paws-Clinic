namespace HappyPaws.Infrastructure.Storage;

/// <summary>
/// <c>Storage:*</c> — where the local adapter keeps files and how it signs its URLs.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// The folder files live in. Empty means a folder under the system temp directory, which is
    /// fine for a laptop and wrong for a server: set it, or replace the adapter, before going live.
    /// </summary>
    public string LocalRoot { get; set; } = string.Empty;

    /// <summary>
    /// The key upload and download URLs are signed with. Empty means one derived from the JWT
    /// signing key, so a development machine needs nothing extra.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>What the URLs start with — the API's own address, with <c>/v1</c>.</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5080/v1";
}
