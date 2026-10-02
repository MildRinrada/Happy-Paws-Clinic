namespace HappyPaws.Api.Http;

/// <summary>
/// Reads and writes the refresh-token cookie.
/// <para>
/// The contract puts the refresh token in an httpOnly cookie for web clients. httpOnly means
/// script cannot read it, which is the entire point: a token JavaScript can reach is a token XSS
/// can exfiltrate, and rotating it would then protect nothing.
/// </para>
/// <para>
/// The consequence is that the browser cannot refresh on its own — the Next.js server holds the
/// cookie and performs the refresh. That is why the frontend routes authenticated calls through
/// its own server rather than calling this API directly.
/// </para>
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "hp_refresh";

    /// <summary>Scoped to the auth endpoints so it is not attached to every ordinary API call.</summary>
    private const string AuthPath = "/auth";

    public static void Write(HttpResponse response, string refreshToken, DateTimeOffset expiresAt, bool secure)
    {
        response.Cookies.Append(Name, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,

            // Strict, not Lax: this cookie only ever needs to travel on calls the app itself
            // makes, never on a top-level navigation from another site.
            SameSite = SameSiteMode.Strict,
            Path = PathFor(response),
            Expires = expiresAt,
            IsEssential = true,
        });
    }

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public static void Clear(HttpResponse response, bool secure) =>
        response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = PathFor(response),
        });

    /// <summary>
    /// The auth endpoints as the caller addressed them. A client that signed in through
    /// <c>/v1/auth/staff/login</c> refreshes through <c>/v1/auth/refresh</c>, and a cookie scoped
    /// to a bare <c>/auth</c> would not be sent there.
    /// </summary>
    private static string PathFor(HttpResponse response) =>
        response.HttpContext.Request.PathBase.Add(AuthPath);
}
