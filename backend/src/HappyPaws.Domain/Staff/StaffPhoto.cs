using System.Text.RegularExpressions;

namespace HappyPaws.Domain.Staff;

/// <summary>
/// The two kinds of value a member's <see cref="StaffMembership.PhotoPath"/> holds
/// (<c>docs/contract-extensions.md § 32</c>), in the one column § 10 gave it.
/// <list type="bullet">
///   <item><description>
///     <b>A file on the website</b>, as § 10 had it: a path under the web app's <c>/public</c>,
///     starting with <c>/</c>, e.g. <c>/images/team/mint.jpg</c>.
///   </description></item>
///   <item><description>
///     <b>An uploaded photo</b>: <see cref="StorageMarker"/> followed by a storage key this class
///     made, e.g. <c>storage:staff-photos/stf_06GFF.../photo-3f2a….jpg</c>. The marker cannot be
///     mistaken for a path, because a path starts with <c>/</c> and § 10 never accepted anything
///     else, so no stored row is read differently than it was written.
///   </description></item>
/// </list>
/// The key names the member it belongs to, so a profile can only ever point at a photo uploaded
/// for that same person.
/// </summary>
public static partial class StaffPhoto
{
    /// <summary>What an uploaded photo's value starts with, in place of a path's <c>/</c>.</summary>
    public const string StorageMarker = "storage:";

    /// <summary>Where uploaded photos live in the file storage.</summary>
    public const string KeyPrefix = "staff-photos/";

    /// <summary>The accepted media types and the file extension each is stored under. No SVG, no GIF.</summary>
    public static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    /// <summary>
    /// A fresh storage key for a new photo of this member: <c>staff-photos/{staff id}/photo-{random}.{ext}</c>.
    /// The random part makes every upload a new file, so a cached copy of the old photo is never
    /// shown under the new name.
    /// </summary>
    /// <param name="staffId">The member the photo is of.</param>
    /// <param name="contentType">One of <see cref="Extensions"/>' keys.</param>
    public static string NewKey(string staffId, string contentType) =>
        $"{KeyPrefix}{staffId}/photo-{Guid.NewGuid():N}.{Extensions[contentType]}";

    /// <summary>The value a profile stores for the photo at <paramref name="key"/>.</summary>
    public static string PhotoPathOf(string key) => StorageMarker + key;

    /// <summary>True when the value says it is an uploaded photo, well formed or not.</summary>
    public static bool IsUploaded(string? photoPath) =>
        photoPath is not null && photoPath.StartsWith(StorageMarker, StringComparison.Ordinal);

    /// <summary>
    /// The storage key of an uploaded photo, or null when <paramref name="photoPath"/> is not one,
    /// or is one this class could not have made.
    /// </summary>
    public static string? StorageKeyOf(string? photoPath) =>
        IsUploaded(photoPath) && UploadedKey().IsMatch(photoPath![StorageMarker.Length..])
            ? photoPath[StorageMarker.Length..]
            : null;

    /// <summary>The member an uploaded photo's value belongs to, or null when it is not a well-formed one.</summary>
    public static string? OwnerOf(string? photoPath) =>
        StorageKeyOf(photoPath) is { } key ? UploadedKey().Match(key).Groups["staff"].Value : null;

    /// <summary>The path of a photo on the website, or null when the value is an uploaded photo or empty.</summary>
    public static string? SitePathOf(string? photoPath) =>
        photoPath is not null && photoPath.StartsWith('/') ? photoPath : null;

    [GeneratedRegex("^staff-photos/(?<staff>stf_[0-9A-Za-z]{1,40})/photo-[0-9a-f]{32}\\.(?:jpg|png|webp)$")]
    private static partial Regex UploadedKey();
}
