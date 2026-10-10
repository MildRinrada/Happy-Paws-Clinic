using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.Photos;

/// <summary>
/// Limits and links for a member's uploaded photo (<c>docs/contract-extensions.md § 32</c>). The
/// value kept in the profile is described by <see cref="StaffPhoto"/>.
/// <para>
/// <b>How the public sees it.</b> As with an article's cover (§ 26): not by a permanent public URL,
/// but by a signed link minted on each read of <c>GET /team</c>, which lists only people shown on
/// the website. A hidden member's photo therefore has no link anywhere a stranger can reach, and a
/// photo taken down stops working within <see cref="PublicLinkLifetime"/>.
/// </para>
/// </summary>
public static class StaffPhotoLinks
{
    /// <summary>A phone photo, comfortably. The same limit as an article's cover.</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Time to send the file after asking for the URL.</summary>
    public static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);

    /// <summary>How long a link on a public page works.</summary>
    public static readonly TimeSpan PublicLinkLifetime = TimeSpan.FromHours(1);

    /// <summary>How long the preview link on the staff screen works.</summary>
    public static readonly TimeSpan StaffLinkLifetime = TimeSpan.FromMinutes(30);

    /// <summary>A signed link to the uploaded photo, or null when the profile has none.</summary>
    public static string? LinkOf(string? photoPath, IFileStorage storage, TimeSpan lifetime) =>
        StaffPhoto.StorageKeyOf(photoPath) is { } key ? storage.CreateDownloadUrl(key, lifetime) : null;
}
