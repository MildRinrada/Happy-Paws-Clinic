using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Scheduling;
using HappyPaws.Api.Http;
using HappyPaws.Application.Scheduling.ChangeShift;
using HappyPaws.Application.Scheduling.CopyWeek;
using HappyPaws.Application.Scheduling.CreateShifts;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Application.Scheduling.GetAssignmentRules;
using HappyPaws.Application.Scheduling.GetScheduleCalendar;
using HappyPaws.Application.Scheduling.GetStaffAvailability;
using HappyPaws.Application.Scheduling.RemoveShift;
using HappyPaws.Application.Scheduling.ReplaceAssignmentRules;
using HappyPaws.Application.Scheduling.RequestTimeOff;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Scheduling;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The roster — "Schedules และ Shifts". "ตารางเวรเป็นตัวกำหนด slot ที่ /booking/availability คำนวณ":
/// what is written here is what booking can offer.
/// <para>
/// Reading is <c>schedule:read</c>, changing is <c>schedule:write</c>. On top of the scope, a
/// branch-bound caller plans only the people and branches they share.
/// </para>
/// <para>
/// Four routes here are not in the API reference: changing and removing one shift
/// (<c>docs/contract-extensions.md § 47</c>), reading the assignment rules back (§ 48) and copying
/// a week (§ 49).
/// </para>
/// </summary>
[ApiController]
[Route("staff/schedules")]
public sealed class SchedulesController : ControllerBase
{
    /// <summary><c>GET /staff/schedules?from=&amp;to=</c> — "ตารางเวรรวมทุกคนแบบปฏิทิน".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">First clinic day (<c>YYYY-MM-DD</c>); today when omitted.</param>
    /// <param name="to">Last clinic day, inclusive; a week from <paramref name="from"/> when omitted.</param>
    /// <param name="branch">Only shifts at this branch.</param>
    /// <param name="staffId">Only this person.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Schedule.Read)]
    [ProducesResponseType<ScheduleCalendarDto>(StatusCodes.Status200OK)]
    public Task<ScheduleCalendarDto> Calendar(
        [FromServices] GetScheduleCalendarHandler handler,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? branch,
        [FromQuery(Name = "staff_id")] string? staffId,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetScheduleCalendarQuery(from, to, branch, staffId), cancellationToken);

    /// <summary><c>POST /staff/schedules/shifts</c> — "สร้างเวรเดี่ยวหรือทั้งสัปดาห์ด้วย RRULE".</summary>
    /// <param name="request">The first shift, an optional break, and an optional RRULE.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("shifts")]
    [RequireScope(Scopes.Schedule.Write)]
    [ProducesResponseType<ListResponse<ShiftDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateShifts(
        [FromBody] CreateShiftsRequest request,
        [FromServices] CreateShiftsHandler handler,
        CancellationToken cancellationToken)
    {
        var shifts = await handler.HandleAsync(
            new CreateShiftsCommand(
                request.StaffId,
                request.BranchId,
                request.StartsAt,
                request.EndsAt,
                request.Break?.StartsAt,
                request.Break?.EndsAt,
                request.Rrule),
            cancellationToken);

        return Created("/staff/schedules", ListResponse.Of(shifts));
    }

    /// <summary>
    /// <c>PATCH /staff/schedules/shifts/{id}</c>: one block at new times. Not in the API reference;
    /// see <c>docs/contract-extensions.md § 47</c>. Answers the block with its new version, and the
    /// appointments in the time it no longer covers.
    /// </summary>
    /// <param name="id">The block.</param>
    /// <param name="request">The new window.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("shifts/{id}")]
    [RequireScope(Scopes.Schedule.Write)]
    [ProducesResponseType<ShiftChangeResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ShiftChangeResultDto>> ChangeShift(
        string id,
        [FromBody] ChangeShiftRequest request,
        [FromServices] ChangeShiftHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangeShiftCommand(id, request.StartsAt, request.EndsAt, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(result.Shift.Version);

        return Ok(result);
    }

    /// <summary>
    /// <c>DELETE /staff/schedules/shifts/{id}</c>: one block off the roster. Not in the API
    /// reference; see <c>docs/contract-extensions.md § 47</c>. Answers <c>200</c> rather than
    /// <c>204</c>, because the appointments that were inside it are the point of the answer.
    /// </summary>
    /// <param name="id">The block.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("shifts/{id}")]
    [RequireScope(Scopes.Schedule.Write)]
    [ProducesResponseType<ShiftRemovedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ShiftRemovedDto> RemoveShift(
        string id,
        [FromServices] RemoveShiftHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new RemoveShiftCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

    /// <summary>
    /// <c>POST /staff/schedules/shifts/copy-week</c>: a branch's week of shifts onto another week,
    /// all or nothing. Not in the API reference; see <c>docs/contract-extensions.md § 49</c>.
    /// </summary>
    /// <param name="request">The branch, both Mondays, and optionally who.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("shifts/copy-week")]
    [RequireScope(Scopes.Schedule.Write)]
    [ProducesResponseType<CopyWeekResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CopyWeek(
        [FromBody] CopyWeekRequest request,
        [FromServices] CopyWeekHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CopyWeekCommand(request.BranchId ?? string.Empty, request.FromWeek, request.ToWeek, request.StaffIds),
            cancellationToken);

        return Created("/staff/schedules", result);
    }

    /// <summary><c>POST /staff/schedules/time-off</c> — "ลา · ระบบเตือนนัดที่ชนและเสนอหมอแทน".</summary>
    /// <param name="request">Who, from, until, and why.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("time-off")]
    [RequireScope(Scopes.Schedule.Write)]
    [ProducesResponseType<TimeOffResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestTimeOff(
        [FromBody] RequestTimeOffRequest request,
        [FromServices] RequestTimeOffHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RequestTimeOffCommand(request.StaffId, request.StartsAt, request.EndsAt, request.Reason),
            cancellationToken);

        return Created("/staff/schedules", result);
    }

    /// <summary><c>GET /staff/schedules/availability</c> — "ช่องว่างจริงหลังหักนัดและเวลาพัก".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="date">The clinic day; today when omitted.</param>
    /// <param name="branch">The branch; may be omitted in a one-branch clinic.</param>
    /// <param name="staffId">Only this person.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("availability")]
    [RequireScope(Scopes.Schedule.Read)]
    [ProducesResponseType<ScheduleAvailabilityDto>(StatusCodes.Status200OK)]
    public Task<ScheduleAvailabilityDto> Availability(
        [FromServices] GetStaffAvailabilityHandler handler,
        [FromQuery] DateOnly? date,
        [FromQuery] string? branch,
        [FromQuery(Name = "staff_id")] string? staffId,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetStaffAvailabilityQuery(date, branch, staffId), cancellationToken);

    /// <summary>
    /// <c>GET /staff/schedules/assignment-rules</c>: the rules as stored, with <c>version</c> and an
    /// <c>ETag</c> to send back as <c>If-Match</c>. Not in the API reference; see
    /// <c>docs/contract-extensions.md § 48</c>. Read on <c>schedule:read</c>, the read side of the
    /// roster the rules belong to.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("assignment-rules")]
    [RequireScope(Scopes.Schedule.Read)]
    [ProducesResponseType<AssignmentRulesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentRulesDto>> GetAssignmentRules(
        [FromServices] GetAssignmentRulesHandler handler,
        CancellationToken cancellationToken)
    {
        var rules = await handler.HandleAsync(cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(rules.Version);

        return Ok(rules);
    }

    /// <summary>
    /// <c>PUT /staff/schedules/assignment-rules</c> — "กติกาจ่ายเคสอัตโนมัติ เช่น หมอแมวรับเคสแมวก่อน".
    /// Replaces the whole set.
    /// </summary>
    /// <param name="request">Every rule, in the order they are tried.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut("assignment-rules")]
    [RequireScope(Scopes.Schedule.Write)]
    [ProducesResponseType<AssignmentRulesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AssignmentRulesDto>> ReplaceAssignmentRules(
        [FromBody] ReplaceAssignmentRulesRequest request,
        [FromServices] ReplaceAssignmentRulesHandler handler,
        CancellationToken cancellationToken)
    {
        var rules = await handler.HandleAsync(
            new ReplaceAssignmentRulesCommand(
                [.. (request.Rules ?? []).Select(rule => new AssignmentRule(rule.Species, rule.ServiceId, rule.StaffIds ?? []))],
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(rules.Version);

        return Ok(rules);
    }
}
