using HappyPaws.Api.Authorization;
using HappyPaws.Application.MedicalRecords.GenerateDocument;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Printed documents from a patient's file.</summary>
[ApiController]
[Route("staff/documents/generate")]
public sealed class DocumentGenerationController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/documents/generate</c> — "ใบรับรองสุขภาพ ใบส่งตัว สมุดวัคซีน เป็น PDF".
    /// <para>
    /// Answers with the PDF itself. A certificate or a referral needs a caller with a licence
    /// number, because their name is printed as the signer. Written to the access log.
    /// </para>
    /// </summary>
    /// <param name="request">Which document, for which pet, and what a referral needs.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Record.Read)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateDocumentRequest request,
        [FromServices] GenerateDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var document = await handler.HandleAsync(
            new GenerateDocumentCommand(
                request.Type,
                request.PetId,
                request.ConsultationId,
                request.ReferTo,
                request.Reason,
                request.Remarks),
            cancellationToken);

        // A health document: not to be kept by a shared cache or a browser's back button.
        Response.Headers.CacheControl = "no-store";

        return File(document.Content, "application/pdf", document.FileName);
    }
}

/// <summary>
/// <c>POST /staff/documents/generate</c>. <c>type</c> is <c>health_certificate</c>,
/// <c>referral_letter</c> or <c>vaccination_book</c>; a referral needs <c>refer_to</c> and
/// <c>reason</c>. Without <c>consultation_id</c> the latest signed visit is used.
/// </summary>
public sealed record GenerateDocumentRequest(
    string Type,
    string PetId,
    string? ConsultationId = null,
    string? ReferTo = null,
    string? Reason = null,
    string? Remarks = null);
