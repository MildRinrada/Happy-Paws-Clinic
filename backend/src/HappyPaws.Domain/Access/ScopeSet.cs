using System.Collections;

namespace HappyPaws.Domain.Access;

/// <summary>
/// A caller's effective scopes, with the contract's two wildcard forms built in.
/// <para>
/// This is the <b>only</b> implementation of "does this caller hold this scope" in the system.
/// The API authorization handler, the <c>/me/capabilities</c> projection and role management all
/// go through it, so a wildcard cannot mean one thing at the security boundary and another in a
/// menu.
/// </para>
/// <list type="bullet">
///   <item><description><c>*</c> — every scope (<c>clinic_owner</c>)</description></item>
///   <item><description><c>resource:*</c> — every action on a resource (<c>consult:*</c>, <c>queue:*</c>)</description></item>
/// </list>
/// Wildcards are expanded at check time, not when a role is saved, so a scope added to the
/// catalogue later is automatically covered by an existing <c>invoice:*</c> grant.
/// </summary>
public sealed class ScopeSet : IReadOnlyCollection<string>
{
    private readonly HashSet<string> _granted;

    public ScopeSet(IEnumerable<string> granted)
    {
        ArgumentNullException.ThrowIfNull(granted);
        _granted = new HashSet<string>(granted, StringComparer.Ordinal);
    }

    public static ScopeSet Empty { get; } = new([]);

    public int Count => _granted.Count;

    /// <summary>True when the caller holds <c>*</c> and therefore every scope.</summary>
    public bool IsUnrestricted => _granted.Contains(Scopes.Wildcard);

    /// <summary>Tests one required scope against the granted set, honouring both wildcard forms.</summary>
    public bool Allows(string requiredScope)
    {
        if (string.IsNullOrEmpty(requiredScope))
        {
            return false;
        }

        if (IsUnrestricted || _granted.Contains(requiredScope))
        {
            return true;
        }

        var resourceWildcard = ResourceWildcardFor(requiredScope);
        return resourceWildcard is not null && _granted.Contains(resourceWildcard);
    }

    /// <summary>Every required scope the caller is missing — this is what fills <c>error.details</c> on a 403.</summary>
    public IReadOnlyList<string> MissingFrom(IEnumerable<string> requiredScopes) =>
        [.. requiredScopes.Where(scope => !Allows(scope))];

    /// <summary><c>consult:write</c> → <c>consult:*</c>. Null when the scope has no resource part.</summary>
    private static string? ResourceWildcardFor(string scope)
    {
        var separator = scope.IndexOf(':', StringComparison.Ordinal);
        return separator <= 0 ? null : string.Concat(scope.AsSpan(0, separator + 1), Scopes.Wildcard);
    }

    public IEnumerator<string> GetEnumerator() => _granted.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
