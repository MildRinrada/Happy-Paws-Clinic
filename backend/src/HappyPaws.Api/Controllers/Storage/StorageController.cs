using HappyPaws.Application.Patients.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Storage;

/// <summary>
/// The local storage adapter's signed URLs — <b>contract extension</b>
/// (<c>docs/contract-extensions.md § 12</c>).
/// <para>
/// These two routes play the part of an S3 bucket. They are not the clinic's API: no scope, no
/// session, because the signed token in the path is the whole of the permission, exactly as with a
/// pre-signed S3 URL. Swapping in a cloud adapter makes the URLs point at the bucket and leaves
/// these routes unused.
/// </para>
/// </summary>
[ApiController]
[Route("storage")]
[AllowAnonymous]
public sealed class StorageController : ControllerBase
{
    /// <summary>Generous for a 10 MB file; the token's own limit is what actually binds.</summary>
    private const long RequestLimit = 11 * 1024 * 1024;

    /// <summary>Receives the file for an upload URL. <c>204</c> when accepted, <c>403</c> for a bad, expired or oversized upload.</summary>
    /// <param name="token">The signed upload token.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut("uploads/{token}")]
    [RequestSizeLimit(RequestLimit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(
        string token,
        [FromServices] StoredFilesHandler handler,
        CancellationToken cancellationToken)
    {
        var accepted = await handler.ReceiveAsync(token, Request.ContentType, Request.Body, cancellationToken);

        // 403 rather than the error envelope: a storage URL answers like storage, and a client
        // that got here holds nothing but the token to try again with.
        return accepted ? NoContent() : StatusCode(StatusCodes.Status403Forbidden);
    }

    /// <summary>Serves the file for a download URL, or <c>404</c> when the token or the file is not good.</summary>
    /// <param name="token">The signed download token.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("files/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        string token,
        [FromServices] StoredFilesHandler handler,
        CancellationToken cancellationToken)
    {
        var file = await handler.OpenAsync(token, cancellationToken);

        if (file is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        // A file stored with a name (a report export), or a link signed with one (a lab's PDF,
        // § 54), downloads under it; any other uploaded file keeps being served without one.
        return file.FileName is { Length: > 0 } name
            ? File(file.Content, file.ContentType, name)
            : File(file.Content, file.ContentType);
    }
}
