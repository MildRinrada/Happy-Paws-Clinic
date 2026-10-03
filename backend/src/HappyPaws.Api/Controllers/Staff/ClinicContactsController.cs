using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Clinics;
using HappyPaws.Api.Http;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Clinics.ManageContacts;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Adding, changing and removing the clinic's contact entries. <b>Not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 15</c>.
/// <para>
/// <c>settings:write</c>, like every other write behind the public clinic pages
/// (<see cref="ClinicSetupController"/>). The read is public, in <c>Public/ClinicController</c>.
/// </para>
/// </summary>
[ApiController]
[Route("clinic/contacts")]
[RequireScope(Scopes.Settings.Write)]
public sealed class ClinicContactsController : ControllerBase
{
    /// <summary><c>POST /clinic/contacts</c> — a new entry.</summary>
    /// <param name="request">What it is, what to show, and optionally a link, a branch and a place in the order.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [ProducesResponseType<ContactDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContactDto>> Add(
        [FromBody] AddContactRequest request,
        [FromServices] AddContactHandler handler,
        CancellationToken cancellationToken)
    {
        var contact = await handler.HandleAsync(
            new AddContactCommand(
                request.Kind,
                request.Label,
                request.Value,
                request.Url,
                request.EmbedUrl,
                request.BranchId,
                request.SortOrder),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(contact.Version);

        return Created("/clinic/contacts", contact);
    }

    /// <summary><c>PATCH /clinic/contacts/{id}</c> — <c>If-Match</c> is honoured when sent.</summary>
    /// <param name="id">Contact id.</param>
    /// <param name="request">The fields to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [ProducesResponseType<ContactDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContactDto>> Update(
        string id,
        [FromBody] UpdateContactRequest request,
        [FromServices] UpdateContactHandler handler,
        CancellationToken cancellationToken)
    {
        var contact = await handler.HandleAsync(
            new UpdateContactCommand(
                id,
                request.Kind,
                request.Label,
                request.Value,
                request.Url,
                request.EmbedUrl,
                request.BranchId,
                request.SortOrder,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(contact.Version);

        return Ok(contact);
    }

    /// <summary><c>DELETE /clinic/contacts/{id}</c> — <c>If-Match</c> is honoured when sent.</summary>
    /// <param name="id">Contact id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        string id,
        [FromServices] RemoveContactHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new RemoveContactCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        return NoContent();
    }
}
