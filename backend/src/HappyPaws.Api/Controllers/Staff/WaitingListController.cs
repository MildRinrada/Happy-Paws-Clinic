using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Application.Appointments.AddToWaitingList;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The waiting list. Only adding is in the contract; entries lapse after their last date.</summary>
[ApiController]
[Route("staff/waiting-list")]
public sealed class WaitingListController : ControllerBase
{
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
}
