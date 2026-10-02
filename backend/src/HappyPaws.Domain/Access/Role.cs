using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Access;

/// <summary>
/// A named bundle of scopes.
/// <para>
/// Roles are data, not code. The contract says a clinic owner defines their own
/// ("ผูกกับ role ที่เจ้าของคลินิกกำหนดเองได้"), which is exactly why authorization checks a scope
/// and never a role name — the moment someone creates "senior nurse", role-name checks are wrong
/// and scope checks are still right.
/// </para>
/// </summary>
public sealed class Role : Entity
{
    private readonly List<string> _scopes = [];

    private Role()
    {
    }

    /// <summary>Stable key used in tokens and seeds, e.g. <c>veterinarian</c>.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// True for the seven starter roles. System roles can have their scopes edited by the owner
    /// but cannot be deleted, so a clinic cannot lock itself out of its own instance.
    /// </summary>
    public bool IsSystem { get; private set; }

    public IReadOnlyList<string> Scopes => _scopes;

    /// <summary>
    /// True for the starter <c>customer</c> role. It describes the portal audience rather than a
    /// job, so it is never assigned to a staff member and its scope set is not editable — the
    /// portal marker comes from holding a customer membership, not from this row.
    /// </summary>
    public bool IsPortalRole => _scopes.Contains(Access.Scopes.Portal);

    /// <summary>True when holding this role means holding every scope — <c>*</c>.</summary>
    public bool IsUnrestricted => _scopes.Contains(Access.Scopes.Wildcard);

    public static Role Create(string name, IEnumerable<string> scopes, bool isSystem = false)
    {
        var role = new Role
        {
            Id = EntityId.New(EntityId.Role),
            Name = name,
            IsSystem = isSystem,
        };

        role._scopes.AddRange(scopes.Distinct(StringComparer.Ordinal));
        return role;
    }

    /// <summary>
    /// Replaces the whole scope set, matching <c>PUT /staff/roles/{id}/permissions</c>.
    /// Unknown scopes are rejected by the caller against the catalogue before this is invoked.
    /// </summary>
    public void ReplaceScopes(IEnumerable<string> scopes)
    {
        _scopes.Clear();
        _scopes.AddRange(scopes.Distinct(StringComparer.Ordinal));
    }
}
