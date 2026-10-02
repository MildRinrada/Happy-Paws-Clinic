using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Patients;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Application.Patients.ManageCarers;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Who may reach a patient — "สัตว์หนึ่งตัวมีผู้ดูแลได้หลายคน".
/// <para>
/// These two routes are the access-control surface of the whole portal. Everything a customer can
/// see about an animal — its appointments, its queue position, its records — is decided by the rows
/// these endpoints write, and the check runs against the database on every request rather than from
/// the token, so a withdrawal takes effect on the next call.
/// </para>
/// </summary>
[ApiController]
[Route("staff/pets/{id}/owners")]
public sealed class PetCarersController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/pets/{id}/owners</c> — "เพิ่มผู้ดูแลร่วม · กำหนดสิทธิ์ดูประวัติ".
    /// <para>
    /// Re-adding an existing carer updates their history permission rather than failing: the intent
    /// is the same either way, and a 409 would make the caller branch on a distinction nobody
    /// outside the database cares about.
    /// </para>
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="request">Who, and whether they may read the clinical history.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Patient.Write)]
    [ProducesResponseType<CarerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarerDto>> Add(
        string id,
        [FromBody] AddCarerRequest request,
        [FromServices] AddCarerHandler handler,
        CancellationToken cancellationToken)
    {
        var carer = await handler.HandleAsync(
            new AddCarerCommand(id, request.CustomerId, request.MayViewHistory), cancellationToken);

        return Created($"/staff/pets/{id}/owners/{carer.CustomerId}", carer);
    }

    /// <summary>
    /// <c>DELETE /staff/pets/{id}/owners/{customerId}</c> —
    /// "ถอนสิทธิ์เข้าถึง ไม่ลบประวัติ".
    /// <para>
    /// The link is closed, never deleted, so who could see what and until when stays answerable —
    /// which matters because withdrawal usually follows a dispute, and a dispute is when someone
    /// asks. The primary carer cannot be removed; transfer first.
    /// </para>
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="customerId">The carer losing access.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{customerId}")]
    [RequireScope(Scopes.Patient.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(
        string id,
        string customerId,
        [FromServices] RemoveCarerHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new RemoveCarerCommand(id, customerId), cancellationToken);

        return NoContent();
    }
}
