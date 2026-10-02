using HappyPaws.Api.Authorization;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Application.Patients.GetWeightHistory;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The weight graph. Read from the vitals of each visit; there is no weights table to write to,
/// which is why this controller has no POST. Weight is recorded with
/// <c>POST /staff/consultations/{id}/vitals</c>.
/// </summary>
[ApiController]
[Route("staff/pets/{id}/weights")]
public sealed class PetWeightsController : ControllerBase
{
    /// <summary><c>GET /staff/pets/{id}/weights</c> — "ชุดข้อมูลกราฟน้ำหนักย้อนหลัง", oldest point first.</summary>
    /// <param name="id">Pet id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="from">The earliest reading to include.</param>
    /// <param name="to">The latest reading to include.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Patient.Read)]
    [ProducesResponseType<WeightSeriesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WeightSeriesDto> Get(
        string id,
        [FromServices] GetWeightHistoryHandler handler,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetWeightHistoryQuery(id, from, to), cancellationToken);
}
