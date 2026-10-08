using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Notifications;
using HappyPaws.Api.Extensions;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Application.Notifications.SendNotification;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Staff sending a template to one or more customers.</summary>
[ApiController]
[Route("staff/notifications")]
public sealed class NotificationsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/notifications/send</c> — "ส่งเดี่ยวหรือเป็นกลุ่ม · เลือก channel ได้".
    /// <para>
    /// Answers <c>200</c> with one result per customer, also for those who were not reached:
    /// <c>skipped</c> with <c>no_consent</c> or <c>no_address</c> is an outcome the counter needs to
    /// see, not an error. Under the contract's messaging rate limit.
    /// </para>
    /// </summary>
    /// <param name="request">The template, the customers, the channel and the values.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("send")]
    [RequireScope(Scopes.Notify.Send)]
    [EnableRateLimiting(RateLimitingExtensions.MessagingPolicy)]
    [ProducesResponseType<SendResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<SendResultDto> Send(
        [FromBody] SendNotificationRequest request,
        [FromServices] SendNotificationHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new SendNotificationCommand(
                request.Template,
                request.CustomerIds ?? [],
                request.Channel,
                request.Variables,
                request.BranchId),
            cancellationToken);
}
