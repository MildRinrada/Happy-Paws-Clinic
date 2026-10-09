using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Application.Appointments.AddToWaitingList;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.ListWaitingList;
using HappyPaws.Application.Appointments.RemoveFromWaitingList;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The waiting list. Only adding is in the contract; listing and removing are
/// <c>docs/contract-extensions.md § 23</c>. Entries also lapse on their own after their last date.
/// </summary>
[ApiController]
[Route("staff/waiting-list")]
public sealed class WaitingListController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/waiting-list?branch=&amp;service_id=&amp;status=&amp;limit=&amp;cursor=</c> — who is
    /// waiting, oldest first. Not in the API reference (§ 23).
    /// <para>
    /// <c>branch</c> gives the entries an opening there would reach (that branch's and those that
    /// name none); <c>status</c> is <c>waiting</c> or <c>lapsed</c>. Branch-bound staff see only
    /// their branches' entries and the ones that name none. On <c>appointment:write</c>, like the
    /// <c>POST</c>: the list is the counter's tool for managing what it added.
    /// </para>
    /// </summary>
    /// <param name="branch">Entries that an opening at this branch would reach.</param>
    /// <param name="serviceId">Only this service.</param>
    /// <param name="status"><c>waiting</c> or <c>lapsed</c>; absent for both.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Appointment.Write)]
    [ProducesResponseType<CursorPageResponse<WaitingListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CursorPageResponse<WaitingListItemDto>>> List(
        [FromQuery] string? branch,
        [FromQuery(Name = "service_id")] string? serviceId,
        [FromQuery] string? status,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListWaitingListHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListWaitingListQuery(branch, serviceId, status, limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        return Ok(CursorPageResponse<WaitingListItemDto>.From(page));
    }

    /// <summary>
    /// <c>POST /staff/waiting-list</c> — "คิวรอคิวว่าง · ยิง LINE อัตโนมัติเมื่อมีคนยกเลิก".
    /// <para>
    /// When a fitting appointment is cancelled, up to three people, in the order they joined, are
    /// sent a message. The slot is not reserved for them; the first to book it has it.
    /// </para>
    /// </summary>
    /// <param name="request">The pet, the service, and the days that would do.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Appointment.Write)]
    [ProducesResponseType<WaitingListEntryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WaitingListEntryDto>> Add(
        [FromBody] AddToWaitingListRequest request,
        [FromServices] AddToWaitingListHandler handler,
        CancellationToken cancellationToken)
    {
        var entry = await handler.HandleAsync(
            new AddToWaitingListCommand(
                request.PetId,
                request.ServiceId,
                request.EarliestOn,
                request.LatestOn,
                request.BranchId,
                request.StaffId,
                request.Note),
            cancellationToken);

        return Created($"/staff/waiting-list/{entry.Id}", entry);
    }

    /// <summary>
    /// <c>DELETE /staff/waiting-list/{id}</c> — takes the pet off the list; nobody is told about
    /// openings for it any more. Answers <c>204</c>; an entry already removed is <c>404</c>. Audited
    /// as <c>waiting_list.removed</c>. Not in the API reference (§ 23).
    /// </summary>
    /// <param name="id">The entry.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{id}")]
    [RequireScope(Scopes.Appointment.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        string id,
        [FromServices] RemoveFromWaitingListHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new RemoveFromWaitingListCommand(id), cancellationToken);

        return NoContent();
    }
}
