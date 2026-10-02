using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Portal;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>Files an owner adds to their own pet's record.</summary>
[ApiController]
[Route("portal/documents")]
[RequireScope(Scopes.Portal)]
public sealed class PortalDocumentsController : ControllerBase
{
    /// <summary>
    /// <c>POST /portal/documents</c> — "อัปโหลดรูปอาการหรือผลจากคลินิกอื่น".
    /// <para>
    /// For a pet the caller cares for; another pet is <c>404</c>. Answers with the document and an
    /// <c>upload</c> to <c>PUT</c> the file to.
    /// </para>
    /// </summary>
    /// <param name="request">The pet, and the file's name, type and size.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [ProducesResponseType<DocumentUploadDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentUploadDto>> Upload(
        [FromBody] UploadOwnerDocumentRequest request,
        [FromServices] UploadOwnerDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var started = await handler.HandleAsync(
            new UploadOwnerDocumentCommand(
                request.PetId,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.Kind,
                request.Title),
            cancellationToken);

        return Created($"/portal/pets/{request.PetId}/records", started);
    }
}
