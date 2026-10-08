using HappyPaws.Api.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Application.Notifications.GetDeliveryLog;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The delivery log.</summary>
[ApiController]
[Route("staff/notifications/log")]
public sealed class NotificationLogController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/notifications/log</c> — "ส่งสำเร็จ/ล้มเหลว · fallback ไป SMS เมื่อไหร่". One row
    /// per attempt, newest first, limited to the caller's branches plus clinic-wide rows.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="status"><c>sent</c>, <c>failed</c> or <c>skipped</c>.</param>
    /// <param name="channel"><c>line</c>, <c>sms</c> or <c>email</c>.</param>
    /// <param name="source"><c>system</c>, <c>manual</c> or <c>broadcast</c>.</param>
    /// <param name="customerId">One customer's messages.</param>
    /// <param name="template">One template key.</param>
    /// <param name="batchId">The rows of one send or broadcast.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Notify.Read)]
    [ProducesResponseType<CursorPage<DeliveryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<CursorPage<DeliveryDto>> List(
        [FromServices] GetDeliveryLogHandler handler,
        [FromQuery] string? status,
        [FromQuery] string? channel,
        [FromQuery] string? source,
        [FromQuery(Name = "customer_id")] string? customerId,
        [FromQuery] string? template,
        [FromQuery(Name = "batch_id")] string? batchId,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new GetDeliveryLogQuery(
                status,
                channel,
                source,
                customerId,
                template,
                batchId,
                limit is 0 ? CursorPaging.DefaultLimit : limit,
                cursor),
            cancellationToken);
}
