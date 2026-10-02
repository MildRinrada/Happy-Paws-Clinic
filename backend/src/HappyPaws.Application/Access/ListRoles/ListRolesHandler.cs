using HappyPaws.Application.Access.Dtos;
using HappyPaws.Domain.Access;

namespace HappyPaws.Application.Access.ListRoles;

/// <summary><c>GET /staff/roles</c> — "role ตั้งต้น + role ที่สร้างเอง".</summary>
public sealed class ListRolesHandler(IRoleRepository roles)
{
    public async Task<IReadOnlyList<RoleDto>> HandleAsync(CancellationToken cancellationToken = default) =>
        [.. (await roles.ListAsync(cancellationToken)).Select(RoleDto.From)];
}

/// <summary>
/// <c>GET /staff/permissions</c> — "แค็ตตาล็อก scope ทั้งหมดที่ระบบรองรับ".
/// <para>
/// Served from <see cref="ScopeCatalogue"/>, the same list <c>PUT /staff/roles/{id}/permissions</c>
/// validates against, so a role editor can never offer a scope the save would then refuse.
/// </para>
/// </summary>
public sealed class ListPermissionCatalogueHandler
{
    public IReadOnlyList<PermissionDto> Handle() => [.. ScopeCatalogue.All.Select(PermissionDto.From)];
}
