using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles;

/// <summary>
/// How an article's cover picture is stored and shown (<c>docs/contract-extensions.md § 26</c>).
/// <para>
/// The file goes through the same signed-upload storage as a pet's documents (§ 12), under a key
/// this class makes: <c>articles/{article id}/cover-{random}.{ext}</c>. The random part means a new
/// cover is a new file, so a cached copy of the old one is never shown under the new name.
/// </para>
/// <para>
/// <b>How the public sees it.</b> Not by a permanent public URL: the public reads answer a signed
/// link that works for <see cref="PublicLinkLifetime"/>, minted on each read and only for an article
/// that is published at that moment. A draft's cover therefore has no link anywhere a stranger can
/// reach, and a cover taken down with its article stops working within the hour. The public pages
/// render on every request, so each visitor gets a fresh link. The pattern is the one the portal
/// already uses for a pet's photo, and it keeps working unchanged if the storage moves to S3.
/// </para>
/// <para>
/// Only JPEG, PNG and WebP. Not SVG, which is a document that can carry script, and not GIF. The
/// file is served with the declared type and <c>X-Content-Type-Options: nosniff</c>, so a browser
/// will not treat it as anything else.
/// </para>
/// </summary>
public static class ArticleCovers
{
    /// <summary>A phone photo, comfortably.</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Time to send the file after asking for the URL.</summary>
    public static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);

    /// <summary>How long a link on a public page works.</summary>
    public static readonly TimeSpan PublicLinkLifetime = TimeSpan.FromHours(1);

    /// <summary>How long the preview link in the editor works.</summary>
    public static readonly TimeSpan StaffLinkLifetime = TimeSpan.FromMinutes(30);

    public const string KeyPrefix = "articles/";

    /// <summary>The accepted media types and the file extension each is stored under.</summary>
    public static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    /// <summary>A fresh key for a new cover of this article.</summary>
    public static string NewKey(string articleId, string contentType) =>
        $"{KeyPrefix}{articleId}/cover-{Guid.NewGuid():N}.{Extensions[contentType]}";

    /// <summary>A signed link to the uploaded cover, or null when there is none.</summary>
    public static string? LinkOf(Article article, IFileStorage storage, TimeSpan lifetime) =>
        article.CoverImageKey is { Length: > 0 } key ? storage.CreateDownloadUrl(key, lifetime) : null;
}
