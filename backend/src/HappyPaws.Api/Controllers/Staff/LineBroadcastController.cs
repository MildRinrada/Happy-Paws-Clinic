using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Notifications;
using HappyPaws.Api.Extensions;
using HappyPaws.Application.Notifications.BroadcastToSegment;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Announcements to a group of customers on LINE.</summary>
[ApiController]
[Route("staff/integrations/line/broadcast")]
public sealed class LineBroadcastController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/integrations/line/broadcast</c> — "ประกาศถึงกลุ่มลูกค้าที่คัดไว้ · จำกัดโควตา".
    /// <para>
    /// Marketing: only customers who agreed to marketing receive it, the rest are listed as
    /// skipped. The group is <c>customer_ids</c> or a saved <c>segment_id</c>, one of the two. <c>409 quota_exceeded</c> when the month's quota would be passed, and
    /// <c>409 line_not_configured</c> when LINE cannot push.
    /// </para>
    /// </summary>
    /// <param name="request">The customers or the segment, and the message.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Notify.Broadcast)]
    [EnableRateLimiting(RateLimitingExtensions.MessagingPolicy)]
    [ProducesResponseType<SendResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<SendResultDto> Broadcast(
        [FromBody] LineBroadcastRequest request,
        [FromServices] BroadcastToSegmentHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new BroadcastToSegmentCommand(request.CustomerIds ?? [], request.Message ?? string.Empty, request.SegmentId),
            cancellationToken);
}
