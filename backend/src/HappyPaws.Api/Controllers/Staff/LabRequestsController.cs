using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Laboratory;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Laboratory.CreateLabRequest;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Application.Laboratory.ListLabRequests;
using HappyPaws.Application.Laboratory.ReviewLabResults;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Laboratory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Lab requests and the worklist. Entering results is in <see cref="LabResultsController"/>,
/// because it takes <c>lab:write</c> — the lab technician's scope — rather than the vet's
/// <c>lab:request</c>.
/// </summary>
[ApiController]
[Route("staff/lab-requests")]
public sealed class LabRequestsController : ControllerBase
{
    /// <summary><c>POST /staff/lab-requests</c> — "ส่งแล็บในคลินิกหรือแล็บภายนอก".</summary>
    /// <param name="request">The patient, the tests, and where the sample goes.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Lab.Request)]
    [ProducesResponseType<LabRequestDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LabRequestDto>> Create(
        [FromBody] CreateLabRequestRequest request,
        [FromServices] CreateLabRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(
            new CreateLabRequestCommand(
                request.PetId,
                request.Destination,
                [.. (request.Tests ?? []).Select(test => new LabTest(test.Name, test.Code))],
                request.ConsultationId,
                request.BranchId,
                request.ExternalLabName,
                request.Priority,
                request.ClinicalNotes),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(created.Version);

        return Created($"/staff/lab-requests/{created.Id}", created);
    }

    /// <summary>
    /// <c>GET /staff/lab-requests?status=pending</c> — "งานค้างของหมอ · ผลที่ยังไม่มีคนอ่าน".
    /// <para>
    /// <c>pending</c> is waiting on the lab and <c>resulted</c> is waiting on a vet;
    /// <c>?requested_by=me</c> narrows to the caller's own requests.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="status"><c>pending</c>, <c>resulted</c> or <c>reviewed</c>.</param>
    /// <param name="petId">One patient's requests.</param>
    /// <param name="requestedBy">A staff id, or <c>me</c>.</param>
    /// <param name="branch">One branch.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Lab.Read)]
    [ProducesResponseType<CursorPage<LabRequestDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<LabRequestDto>> List(
        [FromServices] ListLabRequestsHandler handler,
        [FromQuery] string? status,
        [FromQuery(Name = "pet_id")] string? petId,
        [FromQuery(Name = "requested_by")] string? requestedBy,
        [FromQuery] string? branch,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListLabRequestsQuery(
                status,
                petId,
                requestedBy,
                branch,
                limit is 0 ? CursorPaging.DefaultLimit : limit,
                cursor),
            cancellationToken);

    /// <summary>
    /// <c>POST /staff/lab-requests/{id}/review</c> — <b>contract extension</b>
    /// (<c>docs/contract-extensions.md § 11</c>). Takes the results off "ผลที่ยังไม่มีคนอ่าน".
    /// The body is optional; its <c>note</c> (§ 53, at most 1,000 characters) is kept as
    /// <c>review_note</c>.
    /// </summary>
    /// <param name="id">Lab request id.</param>
    /// <param name="request">The vet's note, or no body.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/review")]
    [RequireScope(Scopes.Lab.Read)]
    [ProducesResponseType<LabRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LabRequestDto>> Review(
        string id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewLabResultsRequest? request,
        [FromServices] ReviewLabResultsHandler handler,
        CancellationToken cancellationToken)
    {
        var reviewed = await handler.HandleAsync(new ReviewLabResultsCommand(id, request?.Note), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(reviewed.Version);

        return Ok(reviewed);
    }
}
