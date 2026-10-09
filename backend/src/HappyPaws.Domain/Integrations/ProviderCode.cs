using System.Text.RegularExpressions;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// The short name of an outside provider in the payment and lab settings: <c>opn</c>,
/// <c>2c2p</c>, <c>idexx</c>. Lowercase letters, digits and underscores, 2 to 30 characters.
/// <para>
/// Free text rather than a fixed list on purpose. No connector to any provider exists in this
/// system yet, so a list would claim support that is not there; the name only records which
/// provider the clinic has an account with, for the connector that will read it.
/// </para>
/// </summary>
public static partial class ProviderCode
{
    public static bool IsValid(string? value) => value is not null && Pattern().IsMatch(value);

    [GeneratedRegex("^[a-z0-9_]{2,30}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
