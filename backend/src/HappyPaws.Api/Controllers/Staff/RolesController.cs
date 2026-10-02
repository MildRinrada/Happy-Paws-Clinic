using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Staff;
using HappyPaws.Api.Http;
using HappyPaws.Application.Access.CreateRole;
using HappyPaws.Application.Access.Dtos;
using HappyPaws.Application.Access.ListRoles;
using HappyPaws.Application.Access.ReplaceRolePermissions;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Roles and the scope catalogue — "สิทธิ์เป็น scope แบบ resource:action ผูกกับ role ที่เจ้าของคลินิก
/// กำหนดเองได้".
/// <para>
/// <c>role:read</c> to look, <c>role:manage</c> to change. <c>role:manage</c> is one of the scopes
/// that requires a second factor, and the use cases add two rules of their own: nobody grants a
/// scope they do not hold, and nothing may leave the clinic without an account holding <c>*</c>.
/// </para>
/// </summary>
[ApiController]
[Route("staff")]
public sealed class RolesController : ControllerBase
{
    /// <summary><c>GET /staff/roles</c> — "role ตั้งต้น + role ที่สร้างเอง".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("roles")]
    [RequireScope(Scopes.Role.Read)]
    [ProducesResponseType<ListResponse<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResponse<RoleDto>>> List(
        [FromServices] ListRolesHandler handler,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(await handler.HandleAsync(cancellationToken)));

    /// <summary><c>POST /staff/roles</c> — "สร้าง role พร้อมชุด scope".</summary>
    /// <param name="request">A name and the scopes it bundles.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("roles")]
    [RequireScope(Scopes.Role.Manage)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] CreateRoleRequest request,
        [FromServices] CreateRoleHandler handler,
        CancellationToken cancellationToken)
    {
        var role = await handler.HandleAsync(new CreateRoleCommand(request.Name, request.Scopes), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(role.Version);

        return Created("/staff/roles", role);
    }

    /// <summary>
    /// <c>PUT /staff/roles/{id}/permissions</c> — "แทนที่ทั้งชุด · ตรวจ scope ที่ไม่มีจริง".
    /// <para>
    /// The body is the role as it should be afterwards. A scope outside the catalogue answers
    /// <c>422</c> and names it. Everyone holding the role changes with it on their next token.
    /// </para>
    /// </summary>
    /// <param name="id">Role id.</param>
    /// <param name="request">The whole replacement scope set.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut("roles/{id}/permissions")]
    [RequireScope(Scopes.Role.Manage)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> ReplacePermissions(
        string id,
        [FromBody] ReplaceRolePermissionsRequest request,
        [FromServices] ReplaceRolePermissionsHandler handler,
        CancellationToken cancellationToken)
    {
        var role = await handler.HandleAsync(
            new ReplaceRolePermissionsCommand(id, request.Scopes, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(role.Version);

        return Ok(role);
    }

    /// <summary>
    /// <c>GET /staff/permissions</c> — "แค็ตตาล็อก scope ทั้งหมดที่ระบบรองรับ". The same list the
    /// two writes above validate against.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    [HttpGet("permissions")]
    [RequireScope(Scopes.Role.Read)]
    [ProducesResponseType<ListResponse<PermissionDto>>(StatusCodes.Status200OK)]
    public ActionResult<ListResponse<PermissionDto>> Permissions(
        [FromServices] ListPermissionCatalogueHandler handler) =>
        Ok(ListResponse.Of(handler.Handle()));
}
