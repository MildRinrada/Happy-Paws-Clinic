using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Patients;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Application.Patients.ListVaccinations;
using HappyPaws.Application.Patients.RecordVaccination;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// A patient's vaccinations. Reading takes <c>patient:read</c> and writing <c>record:write</c>, as
/// the contract assigns them: the vaccination card is shown at the counter, but only someone who
/// writes clinical records may add to it.
/// </summary>
[ApiController]
[Route("staff/pets/{id}/vaccinations")]
public sealed class PetVaccinationsController : ControllerBase
{
    /// <summary><c>GET /staff/pets/{id}/vaccinations</c> — "รอบที่ฉีดแล้วและรอบถัดไปที่ครบกำหนด".</summary>
    /// <param name="id">Pet id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Patient.Read)]
    [ProducesResponseType<PetVaccinationsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PetVaccinationsDto> List(
        string id,
        [FromServices] ListVaccinationsHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ListVaccinationsQuery(id), cancellationToken);

    /// <summary><c>POST /staff/pets/{id}/vaccinations</c> — "ล็อตวัคซีน วันหมดอายุ · ตั้งรอบถัดไปอัตโนมัติ".</summary>
    /// <param name="id">Pet id.</param>
    /// <param name="request">The vaccine, its lot and expiry, and optionally when the next dose is due.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Record.Write)]
    [ProducesResponseType<VaccinationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<VaccinationDto>> Record(
        string id,
        [FromBody] RecordVaccinationRequest request,
        [FromServices] RecordVaccinationHandler handler,
        CancellationToken cancellationToken)
    {
        var dose = await handler.HandleAsync(
            new RecordVaccinationCommand(
                id,
                request.VaccineName,
                request.LotNumber,
                request.VialExpiresOn,
                request.AdministeredAt,
                request.NextDueOn,
                request.ValidForDays,
                request.Manufacturer,
                request.ConsultationId,
                request.Note),
            cancellationToken);

        return Created($"/staff/pets/{id}/vaccinations", dose);
    }
}
