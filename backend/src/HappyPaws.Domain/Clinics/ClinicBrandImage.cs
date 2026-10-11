using System.Text.RegularExpressions;

namespace HappyPaws.Domain.Clinics;

/// <summary>The two pictures a clinic can upload for itself (<c>docs/contract-extensions.md § 50</c>).</summary>
public enum ClinicBrandImageKind
{
    /// <summary>The logo in every place the website and the console draw the clinic's mark. Any shape.</summary>
    Logo,

    /// <summary>The square icon on the browser tab, for a logo too wide to read at 16 px. Optional.</summary>
    TabIcon,
}

/// <summary>
/// An uploaded logo or tab icon as the clinic profile stores it (<c>docs/contract-extensions.md § 50</c>):
/// <see cref="StorageMarker"/> followed by a storage key this class made, e.g.
/// <c>storage:clinic-brand/logo-3f2a….png</c>.
/// <para>
/// The logo lives in <see cref="ClinicProfile.LogoUrl"/>, beside the <c>http(s)</c> address that
/// field always held. The marker cannot be mistaken for an address, because the field never
/// accepted anything but <c>http</c> and <c>https</c>, so no stored row is read differently than it
/// was written. One deployment is one clinic, so the key names no clinic. The key also names its
/// kind, so a tab icon cannot be saved as the logo or the other way round, and anything under
/// another prefix (a staff photo, an article cover, a pet's document) is refused.
/// </para>
/// </summary>
public static partial class ClinicBrandImage
{
    /// <summary>What an uploaded picture's value starts with, in place of an address's scheme.</summary>
    public const string StorageMarker = "storage:";

    /// <summary>Where both pictures live in the file storage.</summary>
    public const string KeyPrefix = "clinic-brand/";

    /// <summary>
    /// The accepted media types and the file extension each is stored under. No SVG: an SVG is a
    /// document that can carry script, and these files are shown on every public page.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    /// <summary>
    /// A fresh storage key: <c>clinic-brand/{logo|tab-icon}-{random}.{ext}</c>. Every upload is a
    /// new file, so a cached copy of the old picture is never shown under the new name.
    /// </summary>
    /// <param name="kind">Which picture.</param>
    /// <param name="contentType">One of <see cref="Extensions"/>' keys.</param>
    public static string NewKey(ClinicBrandImageKind kind, string contentType) =>
        $"{KeyPrefix}{NameOf(kind)}-{Guid.NewGuid():N}.{Extensions[contentType]}";

    /// <summary>The value the profile stores for the picture at <paramref name="key"/>.</summary>
    public static string ValueOf(string key) => StorageMarker + key;

    /// <summary>True when the value says it is an uploaded picture, well formed or not.</summary>
    public static bool IsUploaded(string? value) =>
        value is not null && value.StartsWith(StorageMarker, StringComparison.Ordinal);

    /// <summary>
    /// The storage key of an uploaded picture of this kind, or null when <paramref name="value"/> is
    /// not one, is of the other kind, or is one this class could not have made.
    /// </summary>
    public static string? StorageKeyOf(ClinicBrandImageKind kind, string? value)
    {
        if (!IsUploaded(value))
        {
            return null;
        }

        var match = UploadedKey().Match(value![StorageMarker.Length..]);

        return match.Success && match.Groups["kind"].Value == NameOf(kind) ? match.Value : null;
    }

    private static string NameOf(ClinicBrandImageKind kind) => kind == ClinicBrandImageKind.Logo ? "logo" : "tab-icon";

    [GeneratedRegex("^clinic-brand/(?<kind>logo|tab-icon)-[0-9a-f]{32}\\.(?:jpg|png|webp)$")]
    private static partial Regex UploadedKey();
}
