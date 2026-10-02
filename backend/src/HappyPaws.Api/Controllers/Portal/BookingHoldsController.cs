using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.HoldSlot;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// <c>POST /booking/holds</c> — "จองค้างไว้ 10 นาทีระหว่างกรอกข้อมูล".
/// <para>
/// Marked <c>portal</c> in the contract, so it requires a customer membership. It is not marked
/// idempotent: a hold is cheap, expires by itself, and a client that sends two has simply lost
/// ten minutes of one slot.
/// </para>
/// </summary>
[ApiController]
[Route("booking/holds")]
[RequireScope(Scopes.Portal)]
public sealed class BookingHoldsController : ControllerBase
{
    /// <summary>Reserves a slot while the customer completes the booking form.</summary>
    /// <param name="request">Service, start time and optionally the vet and branch.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [ProducesResponseType<SlotHoldDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SlotHoldDto>> Create(
        [FromBody] HoldSlotRequest request,
        [FromServices] HoldSlotHandler handler,
        CancellationToken cancellationToken)
    {
        var hold = await handler.HandleAsync(
            new HoldSlotCommand(request.ServiceId, request.StartsAt, request.StaffId, request.BranchId),
            cancellationToken);

        return Created($"/booking/holds/{hold.Id}", hold);
    }
}
