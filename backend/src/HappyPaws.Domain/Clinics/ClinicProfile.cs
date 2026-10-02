using System.Text.RegularExpressions;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Clinics;

/// <summary>
/// Who this clinic is — <c>GET /clinic</c>, "ชื่อ โลโก้ สี ใบอนุญาต ผู้ควบคุมกิจการ".
/// <para>
/// One row per instance, for the same reason <see cref="BookingSettings"/> is: one deployment is
/// one clinic, and the fixed id makes a second row impossible rather than merely unexpected.
/// </para>
/// <para>
/// Everything here is public by contract — it is what the clinic's own website prints in its
/// footer — so nothing on this type may ever be a secret. Channel credentials and payment keys are
/// configuration, not profile.
/// </para>
/// </summary>
public sealed partial class ClinicProfile : Entity
{
    public const string SingletonId = "set_clinic";

    private ClinicProfile()
    {
    }

    public string Name { get; private set; } = "Happy Paws Clinic";

    /// <summary>Absolute <c>http(s)</c> address of the logo. Storage of the image itself is not this module's.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>The brand colour as <c>#RRGGBB</c>.</summary>
    public string? BrandColor { get; private set; }

    /// <summary>The animal-hospital licence number ("ใบอนุญาต").</summary>
    public string? LicenseNo { get; private set; }

    /// <summary>The licensed operator of the premises ("ผู้ควบคุมกิจการ").</summary>
    public string? OperatorName { get; private set; }

    public static ClinicProfile CreateDefault() => new() { Id = SingletonId };

    /// <summary>
    /// Applies a partial update. A null argument leaves that field alone; an empty string clears
    /// it — except the name, which a clinic cannot be without.
    /// </summary>
    public void Update(
        string? name = null,
        string? logoUrl = null,
        string? brandColor = null,
        string? licenseNo = null,
        string? operatorName = null)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException("invalid_clinic_profile", "ต้องระบุชื่อคลินิก");
            }

            Name = name.Trim();
        }

        if (logoUrl is not null)
        {
            LogoUrl = Cleared(logoUrl) is { } url ? WebAddress(url) : null;
        }

        if (brandColor is not null)
        {
            BrandColor = Cleared(brandColor) is { } color ? HexColor(color) : null;
        }

        LicenseNo = licenseNo is null ? LicenseNo : Cleared(licenseNo);
        OperatorName = operatorName is null ? OperatorName : Cleared(operatorName);
    }

    /// <summary>
    /// Only <c>http</c> and <c>https</c>. This value is rendered into an <c>img</c> on a public
    /// page, and a stored <c>javascript:</c> or <c>data:</c> address there is a stored XSS.
    /// </summary>
    private static string WebAddress(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.ToString()
            : throw new DomainException("invalid_clinic_profile", "ที่อยู่โลโก้ต้องเป็น URL แบบ http หรือ https");

    private static string HexColor(string value) =>
        HexColorPattern().IsMatch(value)
            ? value.ToUpperInvariant()
            : throw new DomainException("invalid_clinic_profile", "สีต้องอยู่ในรูปแบบ #RRGGBB");

    private static string? Cleared(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorPattern();
}
