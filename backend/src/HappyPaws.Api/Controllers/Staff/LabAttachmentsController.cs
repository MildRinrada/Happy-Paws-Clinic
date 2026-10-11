using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Laboratory;
using HappyPaws.Api.Http;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Application.Laboratory.Files;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Files from the lab on a request — <b>not in the API reference</b>
/// (<c>docs/contract-extensions.md § 54</c>). Putting a file on or taking one off is
/// <c>lab:write</c>; opening one is <c>lab:read</c>. Files are staff-only, and a request at another
/// branch is <c>404</c> on every route here.
/// </summary>
[ApiController]
[Route("staff/lab-requests/{id}/attachments")]
public sealed class LabAttachmentsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/lab-requests/{id}/attachments/uploads</c> — a signed URL to <c>PUT</c> one file
    /// to (PDF, JPEG, PNG or WebP, at most 10 MB, fifteen minutes), and the <c>value</c> that names
    /// it. Nothing on the request changes until <c>POST .../attachments</c> sends that value.
    /// </summary>
    /// <param name="id">Lab request id.</param>
    /// <param name="request">The file's type and size.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("uploads")]
    [RequireScope(Scopes.Lab.Write)]
    [ProducesResponseType<LabFileUploadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<LabFileUploadDto> RequestUpload(
        string id,
        [FromBody] LabFileUploadRequest request,
        [FromServices] RequestLabFileUploadHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new RequestLabFileUploadCommand(id, request.ContentType, request.SizeBytes), cancellationToken);

    /// <summary>
    /// <c>POST /staff/lab-requests/{id}/attachments</c> — puts an uploaded file on the request. The
    /// <c>file</c> must be a value made for this request, and the file must have arrived
    /// (<c>409 invalid_lab_attachment</c> otherwise). At most 20 files. <c>If-Match</c> honoured.
    /// Audited as <c>lab.file_attached</c>.
    /// </summary>
    /// <param name="id">Lab request id.</param>
    /// <param name="request">The uploaded file's value, name and size.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Lab.Write)]
    [ProducesResponseType<LabRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LabRequestDto>> Attach(
        string id,
        [FromBody] AttachLabFileRequest request,
        [FromServices] AttachLabFileHandler handler,
        CancellationToken cancellationToken)
    {
        var updated = await handler.HandleAsync(
            new AttachLabFileCommand(id, request.File, request.FileName, request.SizeBytes, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(updated.Version);

        return Ok(updated);
    }

    /// <summary>
    /// <c>DELETE /staff/lab-requests/{id}/attachments/{attachmentId}</c> — takes a file off the
    /// request and answers the request as it now is. The file stays in storage. <c>If-Match</c>
    /// honoured. Audited as <c>lab.file_removed</c>.
    /// </summary>
    /// <param name="id">Lab request id.</param>
    /// <param name="attachmentId">The file's id (<c>lbf_…</c>).</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{attachmentId}")]
    [RequireScope(Scopes.Lab.Write)]
    [ProducesResponseType<LabRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LabRequestDto>> Remove(
        string id,
        string attachmentId,
        [FromServices] RemoveLabFileHandler handler,
        CancellationToken cancellationToken)
    {
        var updated = await handler.HandleAsync(
            new RemoveLabFileCommand(id, attachmentId, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(updated.Version);

        return Ok(updated);
    }

    /// <summary>
    /// <c>GET /staff/lab-requests/{id}/attachments/{attachmentId}/link</c> — a signed link to the
    /// file, good for five minutes, made now. A PDF downloads rather than opening in the browser.
    /// Written to the access log as <c>pet.lab_file_opened</c>; never cached.
    /// </summary>
    /// <param name="id">Lab request id.</param>
    /// <param name="attachmentId">The file's id (<c>lbf_…</c>).</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{attachmentId}/link")]
    [RequireScope(Scopes.Lab.Read)]
    [ProducesResponseType<LabFileLinkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabFileLinkDto>> Link(
        string id,
        string attachmentId,
        [FromServices] OpenLabFileHandler handler,
        CancellationToken cancellationToken)
    {
        var link = await handler.HandleAsync(new OpenLabFileCommand(id, attachmentId), cancellationToken);

        Response.Headers.CacheControl = "private, no-store";

        return Ok(link);
    }
}
