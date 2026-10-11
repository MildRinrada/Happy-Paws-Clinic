using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.BrandImages;

/// <summary>
/// Limits and links for the clinic's uploaded logo and tab icon (<c>docs/contract-extensions.md § 50</c>).
/// The values kept in the profile are described by <see cref="ClinicBrandImage"/>.
/// <para>
/// <b>How the public sees them.</b> Through two fixed public addresses, <c>GET /clinic/logo</c> and
/// <c>GET /clinic/tab-icon</c>, each a redirect to a signed link minted on that request. Unlike a
/// staff photo (§ 32) a logo is never private: it is what the clinic prints on its own sign. What
/// it needs instead is an address that does not expire, because the browser tab, a phone's home
/// screen and search engines keep it long after the page that named it was drawn.
/// </para>
/// </summary>
public static class BrandImageLinks
{
    /// <summary>The same limit as a staff photo or an article's cover.</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Time to send the file after asking for the URL.</summary>
    public static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How long the link a redirect hands out works. Well past <see cref="RedirectCacheLifetime"/>,
    /// so a browser that kept the redirect still reaches the file.
    /// </summary>
    public static readonly TimeSpan PublicLinkLifetime = TimeSpan.FromHours(1);

    /// <summary>How long a browser or proxy may reuse the redirect before asking again.</summary>
    public static readonly TimeSpan RedirectCacheLifetime = TimeSpan.FromMinutes(5);
}
