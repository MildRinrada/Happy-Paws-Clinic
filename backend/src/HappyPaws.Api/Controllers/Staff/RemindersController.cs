using HappyPaws.Api.Authorization;
using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Automation.ListUpcomingReminders;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The reminders the switched-on rules are about to send.</summary>
[ApiController]
[Route("staff/reminders")]
public sealed class RemindersController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/reminders/upcoming</c> — "คิวข้อความที่จะถูกส่งใน 7 วันข้างหน้า". Every reminder
    /// an enabled rule will send over the next <c>days</c> (7 by default, at most 30), earliest
    /// first, limited to the reader's branches. Computed by the same evaluator as the real run.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="days">How far ahead.</param>
    /// <param name="branch">Only this branch's reminders.</param>
    /// <param name="automationId">Only this rule's reminders.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("upcoming")]
    [RequireScope(Scopes.Automation.Read)]
    [ProducesResponseType<UpcomingRemindersDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<UpcomingRemindersDto> Upcoming(
        [FromServices] ListUpcomingRemindersHandler handler,
        [FromQuery] int? days,
        [FromQuery] string? branch,
        [FromQuery(Name = "automation_id")] string? automationId,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ListUpcomingRemindersQuery(days, branch, automationId), cancellationToken);
}
