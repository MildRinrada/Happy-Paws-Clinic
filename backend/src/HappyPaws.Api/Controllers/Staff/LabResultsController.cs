using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Laboratory;
using HappyPaws.Api.Http;
using HappyPaws.Application.Laboratory.CorrectLabResults;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Application.Laboratory.RecordLabResults;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Laboratory;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Entering results, under <c>lab:write</c>. Correcting or striking out one value is here too,
/// as a <b>contract extension</b> (<c>docs/contract-extensions.md § 52</c>): the person who may
/// enter a value may enter it again.
/// </summary>
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

    /// <summary>
    /// <c>POST /staff/lab-requests/{id}/results/{resultId}/amend</c> — corrects one value. The body
    /// is the whole value as it should have been, plus a required <c>reason</c>. The value replaced
    /// is kept in <c>previous_values</c>; read results go back to <c>resulted</c>. <c>If-Match</c>
    /// honoured. Audited as <c>lab.result_amended</c>. Not in the API reference (§ 52).
    /// </summary>
    /// <param name="id">Lab request id.</param>
    /// <param name="resultId">The value's id (<c>lbr_…</c>).</param>
    /// <param name="request">The corrected value and why.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{resultId}/amend")]
    [RequireScope(Scopes.Lab.Write)]
    [ProducesResponseType<LabRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LabRequestDto>> Amend(
        string id,
        string resultId,
        [FromBody] AmendLabResultRequest request,
        [FromServices] AmendLabResultHandler handler,
        CancellationToken cancellationToken)
    {
        var updated = await handler.HandleAsync(
            new AmendLabResultCommand(
                id,
                resultId,
                new LabResultInput(
                    request.Analyte ?? string.Empty,
                    request.Value,
                    request.ValueText,
                    request.Unit,
                    request.ReferenceLow,
                    request.ReferenceHigh,
                    request.ReferenceText,
                    request.Abnormal),
                request.Reason,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(updated.Version);

        return Ok(updated);
    }

    /// <summary>
    /// <c>POST /staff/lab-requests/{id}/results/{resultId}/void</c> — strikes one value out, with a
    /// required <c>reason</c>. It stays listed with <c>voided: true</c> and counts towards nothing.
    /// Voiding twice is <c>409 state_conflict</c>. <c>If-Match</c> honoured. Audited as
    /// <c>lab.result_voided</c>. Not in the API reference (§ 52).
    /// </summary>
    /// <param name="id">Lab request id.</param>
    /// <param name="resultId">The value's id (<c>lbr_…</c>).</param>
    /// <param name="request">Why.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{resultId}/void")]
    [RequireScope(Scopes.Lab.Write)]
    [ProducesResponseType<LabRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LabRequestDto>> Void(
        string id,
        string resultId,
        [FromBody] VoidLabResultRequest request,
        [FromServices] VoidLabResultHandler handler,
        CancellationToken cancellationToken)
    {
        var updated = await handler.HandleAsync(
            new VoidLabResultCommand(id, resultId, request.Reason, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(updated.Version);

        return Ok(updated);
    }
}
