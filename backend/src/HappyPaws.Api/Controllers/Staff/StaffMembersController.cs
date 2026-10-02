using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Staff;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Staff.DeactivateStaffMember;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Application.Staff.ListDepartments;
using HappyPaws.Application.Staff.ListStaffMembers;
using HappyPaws.Application.Staff.ListStaffTypes;
using HappyPaws.Application.Staff.ReplacePublicProfile;
using HappyPaws.Application.Staff.UpdateStaffMember;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The people who work here — "เชิญ · เปิดใช้งาน · ปิดใช้งาน · กำหนดสิทธิ์เอง".
/// <para>
/// Reading the list is <c>staff:read</c>; changing anyone on it is <c>staff:manage</c>. On top of
/// the scope, the use cases hold a manager to their own branches and to people who hold no more
/// than they do — see <c>StaffMemberAccess</c>.
/// </para>
/// </summary>
[ApiController]
[Route("staff/members")]
public sealed class StaffMembersController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/members</c> — "กรองตามสาขา ตำแหน่ง สถานะ".
    /// <para>
    /// <c>?status=invited</c> lists invitations nobody has accepted yet, in the same shape: the
    /// contract lets a clinic invite someone and has no other endpoint that says who is still
    /// outstanding.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="branch">Members assigned to this branch.</param>
    /// <param name="staffType">Members with this position.</param>
    /// <param name="status"><c>active</c>, <c>deactivated</c> or <c>invited</c>.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="department">Members in this department. Extension, § 10.</param>
    /// <param name="role">Members holding the role with this name. Extension, § 10.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Staff.Read)]
    [ProducesResponseType<CursorPage<StaffMemberDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<StaffMemberDto>> List(
        [FromServices] ListStaffMembersHandler handler,
        [FromQuery] string? branch,
        [FromQuery(Name = "staff_type")] string? staffType,
        [FromQuery] string? status,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromQuery] string? department,
        [FromQuery] string? role,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListStaffMembersQuery(
                branch, staffType, status, limit is 0 ? CursorPaging.DefaultLimit : limit, cursor, department, role),
            cancellationToken);

    /// <summary>
    /// <c>PATCH /staff/members/{id}</c> — "ตำแหน่ง สาขา เลขใบประกอบวิชาชีพ".
    /// <para>
    /// Also where a member's roles change, there being no other endpoint that could. A caller may
    /// not assign a role that holds more than they do, and the change is refused if it would leave
    /// nobody with <c>*</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Staff id.</param>
    /// <param name="request">The fields to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [RequireScope(Scopes.Staff.Manage)]
    [ProducesResponseType<StaffMemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffMemberDto>> Update(
        string id,
        [FromBody] UpdateStaffMemberRequest request,
        [FromServices] UpdateStaffMemberHandler handler,
        CancellationToken cancellationToken)
    {
        var member = await handler.HandleAsync(
            new UpdateStaffMemberCommand(
                id,
                request.StaffType,
                request.Roles,
                request.Branches,
                request.LicenseNo,
                IfMatch.Parse(Request.Headers.IfMatch),
                request.Department),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(member.Version);

        return Ok(member);
    }

    /// <summary>
    /// <c>PUT /staff/members/{id}/public-profile</c>. What the public team page says about this
    /// member, and whether it lists them at all. <b>Contract extension</b>, see
    /// <c>docs/contract-extensions.md § 10</c>.
    /// </summary>
    /// <param name="id">Staff id.</param>
    /// <param name="request">The whole profile; a field left out is cleared.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut("{id}/public-profile")]
    [RequireScope(Scopes.Staff.Manage)]
    [ProducesResponseType<StaffMemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffMemberDto>> ReplacePublicProfile(
        string id,
        [FromBody] ReplacePublicProfileRequest request,
        [FromServices] ReplacePublicProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var member = await handler.HandleAsync(
            new ReplacePublicProfileCommand(
                id,
                new StaffPublicProfile(
                    request.JobTitle,
                    request.Specialty,
                    request.Bio,
                    request.PhotoPath,
                    request.CareerStartYear,
                    request.ShowOnWebsite),
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(member.Version);

        return Ok(member);
    }

    /// <summary>
    /// <c>POST /staff/members/{id}/deactivate</c> — "ปิดสิทธิ์ทันที แต่เก็บชื่อในเวชระเบียนเดิม".
    /// <para>
    /// Every session is revoked and no new token can be issued. The row and the account stay, so
    /// records the person signed still name them.
    /// </para>
    /// </summary>
    /// <param name="id">Staff id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/deactivate")]
    [RequireScope(Scopes.Staff.Manage)]
    [ProducesResponseType<StaffMemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<StaffMemberDto> Deactivate(
        string id,
        [FromServices] DeactivateStaffMemberHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new DeactivateStaffMemberCommand(id), cancellationToken);

    /// <summary>
    /// <c>GET /staff/staff-types</c> — "ประเภทพนักงานที่คลินิกนิยามเอง". The positions in use here,
    /// with how many active members hold each.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("/staff/staff-types")]
    [RequireScope(Scopes.Staff.Read)]
    [ProducesResponseType<ListResponse<StaffTypeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResponse<StaffTypeDto>>> StaffTypes(
        [FromServices] ListStaffTypesHandler handler,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(await handler.HandleAsync(cancellationToken)));

    /// <summary>
    /// <c>GET /staff/departments</c>. Every department with how many active members are in it.
    /// <b>Contract extension</b>, see <c>docs/contract-extensions.md § 10</c>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("/staff/departments")]
    [RequireScope(Scopes.Staff.Read)]
    [ProducesResponseType<ListResponse<DepartmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResponse<DepartmentDto>>> ListDepartments(
        [FromServices] ListDepartmentsHandler handler,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(await handler.HandleAsync(cancellationToken)));
}
