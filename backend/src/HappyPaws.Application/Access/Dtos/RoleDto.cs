using HappyPaws.Domain.Access;

namespace HappyPaws.Application.Access.Dtos;

/// <summary>
/// A role as <c>GET /staff/roles</c> lists it — "role ตั้งต้น + role ที่สร้างเอง".
/// </summary>
/// <param name="Id">Used in <c>PUT /staff/roles/{id}/permissions</c>.</param>
/// <param name="Version">Echo in <c>If-Match</c> when replacing the scope set.</param>
/// <param name="Name">The key the rest of the API speaks — <c>"roles": ["veterinarian"]</c>.</param>
/// <param name="IsSystem">True for the contract's seven starter roles.</param>
/// <param name="Scopes">As stored, wildcards unexpanded, so a screen can show <c>invoice:*</c> as one grant.</param>
/// <param name="Assignable">False for the <c>customer</c> role, which describes the portal and is never given to staff.</param>
public sealed record RoleDto(
    string Id,
    uint Version,
    string Name,
    bool IsSystem,
    IReadOnlyList<string> Scopes,
    bool Assignable)
{
    public static RoleDto From(Role role) =>
        new(role.Id, role.Version, role.Name, role.IsSystem, role.Scopes, !role.IsPortalRole);
}

/// <summary>One entry of <c>GET /staff/permissions</c> — "แค็ตตาล็อก scope ทั้งหมดที่ระบบรองรับ".</summary>
public sealed record PermissionDto(string Scope, string Resource, string Action)
{
    public static PermissionDto From(string scope) =>
        new(scope, ScopeCatalogue.ResourceOf(scope), ScopeCatalogue.ActionOf(scope));
}
