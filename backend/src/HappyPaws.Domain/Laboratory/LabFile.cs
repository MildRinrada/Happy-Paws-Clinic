using System.Text.RegularExpressions;

namespace HappyPaws.Domain.Laboratory;

/// <summary>
/// The storage keys of files attached to lab requests (<c>docs/contract-extensions.md § 54</c>):
/// <c>lab-files/{lab request id}/file-{random}.{ext}</c>, and on the wire the same key after
/// <see cref="StorageMarker"/>, the way § 32 names an uploaded photo.
/// <para>
/// The key names the request it was made for and the one type it was made for, so attaching
/// checks both from the key alone: a file uploaded for one request cannot be attached to another,
/// and the type served is the type the upload URL allowed.
/// </para>
/// </summary>
public static partial class LabFile
{
    /// <summary>What a key starts with on the wire.</summary>
    public const string StorageMarker = "storage:";

    /// <summary>Where the files live in the file storage.</summary>
    public const string KeyPrefix = "lab-files/";

    /// <summary>A scanned report or a long PDF; larger is not a report.</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>Files one request may hold.</summary>
    public const int MaxPerRequest = 20;

    public const int MaxFileNameLength = 200;

    /// <summary>The accepted media types and the extension each is stored under. No SVG, no HTML, no HEIC.</summary>
    public static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["application/pdf"] = "pdf",
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    /// <summary>A fresh key for a new file on this request. The random part makes every upload a new file.</summary>
    /// <param name="labRequestId">The request the file is for.</param>
    /// <param name="contentType">One of <see cref="Extensions"/>' keys.</param>
    public static string NewKey(string labRequestId, string contentType) =>
        $"{KeyPrefix}{labRequestId}/file-{Guid.NewGuid():N}.{Extensions[contentType]}";

    /// <summary>The wire value of a key: <c>storage:lab-files/…</c>.</summary>
    public static string ValueOf(string key) => StorageMarker + key;

    /// <summary>The key inside a wire value, or null when the value is not one this class could have made.</summary>
    public static string? KeyOf(string? value) =>
        value is not null && value.StartsWith(StorageMarker, StringComparison.Ordinal) && Key().IsMatch(value[StorageMarker.Length..])
            ? value[StorageMarker.Length..]
            : null;

    /// <summary>The request a key was made for, or null when it is not a well-formed key.</summary>
    public static string? RequestOf(string key) =>
        Key().Match(key) is { Success: true } match ? match.Groups["lab"].Value : null;

    /// <summary>The media type a key was made for, or null when it is not a well-formed key.</summary>
    public static string? ContentTypeOf(string key) =>
        Key().Match(key) is { Success: true } match
            ? Extensions.First(pair => pair.Value == match.Groups["ext"].Value).Key
            : null;

    [GeneratedRegex("^lab-files/(?<lab>lab_[0-9A-Za-z]{1,40})/file-[0-9a-f]{32}\\.(?<ext>pdf|jpg|png|webp)$")]
    private static partial Regex Key();
}
