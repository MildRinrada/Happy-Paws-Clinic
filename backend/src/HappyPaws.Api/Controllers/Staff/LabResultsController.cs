using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Laboratory;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Application.Laboratory.RecordLabResults;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Laboratory;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Entering results, under <c>lab:write</c>.</summary>
[ApiController]
[Route("staff/lab-requests/{id}/results")]
public sealed class LabResultsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/lab-requests/{id}/results</c> — "ค่าที่วัดได้ + ช่วงอ้างอิง · ธงค่าผิดปกติ".
    /// May be called again as more results arrive; each call adds to what is there.
    /// </summary>
    /// <param name="id">Lab request id.</param>
    /// <param name="request">The measured values with their ranges.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Lab.Write)]
    [ProducesResponseType<LabRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<LabRequestDto> Record(
        string id,
        [FromBody] RecordLabResultsRequest request,
        [FromServices] RecordLabResultsHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new RecordLabResultsCommand(
                id,
                [.. (request.Results ?? []).Select(result => new LabResultInput(
                    result.Analyte,
                    result.Value,
                    result.ValueText,
                    result.Unit,
                    result.ReferenceLow,
                    result.ReferenceHigh,
                    result.ReferenceText,
                    result.Abnormal))]),
            cancellationToken);
}
