using HappyPaws.Api.Authorization;
using HappyPaws.Application.Prescriptions.DispensePrescription;
using HappyPaws.Application.Prescriptions.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Dispensing, under <c>rx:dispense</c> — the pharmacy's scope, separate from writing.</summary>
[ApiController]
[Route("staff/prescriptions/{id}/dispense")]
public sealed class PrescriptionDispenseController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/prescriptions/{id}/dispense</c> — "จ่ายยาจริง · ตัดสต๊อกและพิมพ์ฉลาก".
    /// <para>
    /// Answers with the label to print. <c>stock_deducted</c> is false until the Inventory module
    /// exists; see the handler. A prescription already dispensed answers <c>409</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Prescription id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Prescription.Dispense)]
    [ProducesResponseType<DispenseResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<DispenseResultDto> Dispense(
        string id,
        [FromServices] DispensePrescriptionHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new DispensePrescriptionCommand(id), cancellationToken);
}
