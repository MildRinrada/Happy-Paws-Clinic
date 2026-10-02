using System.Globalization;

namespace HappyPaws.Api.Http;

/// <summary>
/// Parses and formats the <c>If-Match</c> / <c>ETag</c> value that carries a resource's
/// <c>version</c>.
/// <para>
/// The version is transported as a quoted ETag (<c>"8412"</c>) because that is what the HTTP
/// header is defined to hold and what proxies and clients already understand. The wire form is an
/// ETag; the domain form is the <c>xmin</c> counter behind <c>Entity.Version</c>.
/// </para>
/// </summary>
public static class IfMatch
{
    /// <summary>
    /// Reads a version out of an <c>If-Match</c> header, tolerating quotes and the weak prefix.
    /// Returns null when the header is absent, wildcard, or not a version — a malformed header is
    /// treated as absent so that a broken client fails the explicit "If-Match required" check with
    /// a clear message rather than a parse error.
    /// </summary>
    public static uint? Parse(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue) || headerValue == "*")
        {
            return null;
        }

        var value = headerValue.Trim();

        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        value = value.Trim('"');

        return uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var version)
            ? version
            : null;
    }

    /// <summary>Formats a version as the <c>ETag</c> to return alongside a resource.</summary>
    public static string ToETag(uint version) =>
        string.Concat("\"", version.ToString(CultureInfo.InvariantCulture), "\"");
}
