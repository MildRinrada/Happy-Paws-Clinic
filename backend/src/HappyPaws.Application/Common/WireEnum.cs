namespace HappyPaws.Application.Common;

/// <summary>
/// Enums on the wire: <c>InHouse</c> crosses as <c>in_house</c>, and <c>in_house</c> parses back.
/// <para>
/// For the enums whose wire names are just their own names in snake case. The older modules keep
/// their hand-written maps where a wire name differs from the C# one.
/// </para>
/// </summary>
public static class WireEnum
{
    public static string Of<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        var builder = new System.Text.StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Parses a wire name. Numbers are refused: <c>Enum.TryParse</c> would accept <c>"1"</c>, and a
    /// client that sends a number has misunderstood the field.
    /// </summary>
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsDigit))
        {
            return false;
        }

        return Enum.TryParse(value.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true, out result)
            && Enum.IsDefined(result);
    }

    /// <summary>The accepted names, for an error message that tells the caller what would have worked.</summary>
    public static string Names<TEnum>()
        where TEnum : struct, Enum =>
        string.Join(", ", Enum.GetValues<TEnum>().Select(Of));
}
