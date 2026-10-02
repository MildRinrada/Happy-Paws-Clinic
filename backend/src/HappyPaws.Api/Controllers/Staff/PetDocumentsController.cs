using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Patients;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Files on a patient's record, added by the clinic.</summary>
[ApiController]
[Route("staff/pets/{id}/documents")]
public sealed class PetDocumentsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/pets/{id}/documents</c> — "อัปโหลดผ่าน pre-signed URL · เลือกให้เจ้าของเห็นได้".
    /// <para>
    /// Answers with the document and an <c>upload</c> to <c>PUT</c> the file to. JPEG, PNG, WEBP,
    /// HEIC or PDF, at most 10 MB; the URL lasts fifteen minutes.
    /// </para>
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="request">The file's name, type and size, and who may see it.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Record.Write)]
    [ProducesResponseType<DocumentUploadDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentUploadDto>> Create(
        string id,
        [FromBody] CreatePetDocumentRequest request,
        [FromServices] CreateDocumentUploadUrlHandler handler,
        CancellationToken cancellationToken)
    {
        var started = await handler.HandleAsync(
            new CreateDocumentUploadUrlCommand(
                id,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.Kind,
                request.Title,
                request.VisibleToOwner,
                request.ConsultationId),
            cancellationToken);

        return Created($"/staff/pets/{id}/documents/{started.Document.Id}", started);
    }
}
