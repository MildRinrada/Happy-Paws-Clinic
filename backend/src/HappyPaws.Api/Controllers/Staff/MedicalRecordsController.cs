using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.MedicalRecords;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.MedicalRecords.AmendMedicalRecord;
using HappyPaws.Application.MedicalRecords.Dtos;
using HappyPaws.Application.MedicalRecords.GetMedicalRecord;
using HappyPaws.Application.MedicalRecords.GetRecordAudit;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Signed records: read one, correct one, and review what happened to it.
/// <para>
/// Three routes, three scopes, on purpose. <c>record:read</c> opens a patient's file — and every
/// such read is written to the access log. <c>record:amend</c> corrects it, never in place.
/// <c>record:audit</c> reviews who has done either, which is a supervisory power rather than a
/// clinical one.
/// </para>
/// </summary>
[ApiController]
[Route("staff/medical-records")]
public sealed class MedicalRecordsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/medical-records/{id}</c> — "ฉบับที่ sign แล้วพร้อมลายเซ็นและเวลา".
    /// <para>
    /// This read is audited. Opening a patient's file leaves a row naming who did it, from which
    /// address, and when — readable through <c>GET /staff/audit-logs/access</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Medical record id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [RequireScope(Scopes.Record.Read)]
    [ProducesResponseType<MedicalRecordDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalRecordDto>> Get(
        string id,
        [FromServices] GetMedicalRecordHandler handler,
        CancellationToken cancellationToken)
    {
        var record = await handler.HandleAsync(new GetMedicalRecordQuery(id), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(record.Version);

        return Ok(record);
    }

    /// <summary>
    /// <c>POST /staff/medical-records/{id}/amendments</c> —
    /// "แก้ไขแบบมีร่องรอย · เก็บฉบับเดิมไว้เสมอ".
    /// </summary>
    /// <param name="id">Medical record id.</param>
    /// <param name="request">The reason and the field changes.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/amendments")]
    [RequireScope(Scopes.Record.Amend)]
    [ProducesResponseType<MedicalRecordDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MedicalRecordDto>> Amend(
        string id,
        [FromBody] AmendRecordRequest request,
        [FromServices] AmendMedicalRecordHandler handler,
        CancellationToken cancellationToken)
    {
        var record = await handler.HandleAsync(
            new AmendMedicalRecordCommand(
                id, request.Reason, request.Changes, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(record.Version);

        return Created($"/staff/medical-records/{record.Id}", record);
    }

    /// <summary>
    /// <c>GET /staff/medical-records/{id}/audit</c> —
    /// "ใครแก้อะไรเมื่อไหร่ · diff รายฟิลด์".
    /// </summary>
    /// <param name="id">Medical record id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="limit">Page size for the audit entries.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}/audit")]
    [RequireScope(Scopes.Record.Audit)]
    [ProducesResponseType<RecordAuditDto>(StatusCodes.Status200OK)]
    public Task<RecordAuditDto> Audit(
        string id,
        [FromServices] GetRecordAuditHandler handler,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new GetRecordAuditQuery(id, limit is 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);
}
