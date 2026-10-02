using HappyPaws.Api.Authorization;
using HappyPaws.Application.Auditing.Dtos;
using HappyPaws.Application.Auditing.ListAuditLogs;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The two audit readers. Both require <c>audit:read</c> and are additionally branch-scoped by the
/// handler.
/// </summary>
[ApiController]
[Route("staff/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/audit-logs</c> — "ใคร ทำอะไร กับเรกคอร์ดไหน เมื่อไหร่ จาก IP ใด".
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="action">Narrow to one audited action.</param>
    /// <param name="resourceType">Narrow to one kind of resource.</param>
    /// <param name="resourceId">Narrow to one row.</param>
    /// <param name="petId">Narrow to one patient.</param>
    /// <param name="actorUserId">Narrow to one person's activity.</param>
    /// <param name="from">Inclusive lower bound.</param>
    /// <param name="to">Exclusive upper bound.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Audit.Read)]
    [ProducesResponseType<CursorPage<AuditEntryDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<AuditEntryDto>> List(
        [FromServices] ListAuditLogsHandler handler,
        [FromQuery(Name = "action")] string? action,
        [FromQuery(Name = "resource_type")] string? resourceType,
        [FromQuery(Name = "resource_id")] string? resourceId,
        [FromQuery(Name = "pet_id")] string? petId,
        [FromQuery(Name = "actor_user_id")] string? actorUserId,
        [FromQuery(Name = "from")] DateTimeOffset? from,
        [FromQuery(Name = "to")] DateTimeOffset? to,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListAuditLogsQuery(
                AccessOnly: false,
                action,
                resourceType,
                resourceId,
                petId,
                actorUserId,
                from,
                to,
                limit is 0 ? CursorPaging.DefaultLimit : limit,
                cursor),
            cancellationToken);

    /// <summary>
    /// <c>GET /staff/audit-logs/access</c> — "การเปิดดูเวชระเบียนของคนไข้แต่ละราย".
    /// <para>
    /// The same table, filtered to reads. Kept as its own route because it answers a privacy
    /// question rather than a change-control one, and because it is the view a data subject is
    /// entitled to be shown.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="petId">The patient whose file access is being reviewed.</param>
    /// <param name="actorUserId">Narrow to one person's viewing.</param>
    /// <param name="from">Inclusive lower bound.</param>
    /// <param name="to">Exclusive upper bound.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("access")]
    [RequireScope(Scopes.Audit.Read)]
    [ProducesResponseType<CursorPage<AuditEntryDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<AuditEntryDto>> Access(
        [FromServices] ListAuditLogsHandler handler,
        [FromQuery(Name = "pet_id")] string? petId,
        [FromQuery(Name = "actor_user_id")] string? actorUserId,
        [FromQuery(Name = "from")] DateTimeOffset? from,
        [FromQuery(Name = "to")] DateTimeOffset? to,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListAuditLogsQuery(
                AccessOnly: true,
                PetId: petId,
                ActorUserId: actorUserId,
                From: from,
                To: to,
                Limit: limit is 0 ? CursorPaging.DefaultLimit : limit,
                Cursor: cursor),
            cancellationToken);
}
