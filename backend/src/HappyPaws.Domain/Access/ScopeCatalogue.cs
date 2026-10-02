using System.Reflection;

namespace HappyPaws.Domain.Access;

/// <summary>
/// The closed list of scopes this instance understands — what <c>GET /staff/permissions</c> serves
/// ("แค็ตตาล็อก scope ทั้งหมดที่ระบบรองรับ") and what <c>PUT /staff/roles/{id}/permissions</c>
/// validates against ("ตรวจ scope ที่ไม่มีจริง").
/// <para>
/// Read off the constants in <see cref="Scopes"/> by reflection rather than typed out a second
/// time. A hand-kept list would be one forgotten line away from a scope that endpoints require and
/// no role can be granted.
/// </para>
/// <para>
/// <see cref="Scopes.Portal"/> is deliberately absent. It marks the customer surface and comes from
/// holding a customer membership, never from a role a clinic can edit.
/// </para>
/// </summary>
public static class ScopeCatalogue
{
    private const string WildcardSuffix = ":" + Scopes.Wildcard;

    /// <summary>Every <c>resource:action</c> scope, in a stable order.</summary>
    public static IReadOnlyList<string> All { get; } = Discover();

    private static readonly HashSet<string> Known = new(All, StringComparer.Ordinal);

    private static readonly HashSet<string> Resources = new(
        All.Select(ResourceOf), StringComparer.Ordinal);

    /// <summary><c>consult:write</c> → <c>consult</c>.</summary>
    public static string ResourceOf(string scope) => scope[..scope.IndexOf(':', StringComparison.Ordinal)];

    /// <summary><c>consult:write</c> → <c>write</c>.</summary>
    public static string ActionOf(string scope) => scope[(scope.IndexOf(':', StringComparison.Ordinal) + 1)..];

    /// <summary>
    /// True when a role may carry this value: a catalogue scope, or one of the contract's two
    /// wildcard forms (<c>*</c>, <c>consult:*</c>) over a resource that exists.
    /// </summary>
    public static bool IsGrantable(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return false;
        }

        if (scope == Scopes.Wildcard || Known.Contains(scope))
        {
            return true;
        }

        return scope.EndsWith(WildcardSuffix, StringComparison.Ordinal)
            && Resources.Contains(scope[..^WildcardSuffix.Length]);
    }

    /// <summary>The values in <paramref name="scopes"/> that no role may carry, for the refusal's details.</summary>
    public static IReadOnlyList<string> UnknownIn(IEnumerable<string> scopes) =>
        [.. scopes.Where(scope => !IsGrantable(scope)).Distinct(StringComparer.Ordinal)];

    private static List<string> Discover() =>
        [.. typeof(Scopes)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(group => group.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope, StringComparer.Ordinal)];
}
