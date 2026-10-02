using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Portal;
using HappyPaws.Api.Http;
using HappyPaws.Application.Patients.Portal;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// The owner's pets. Every route is narrowed to the pets the caller cares for; another pet is
/// <c>404</c>. The clinical history is in <see cref="PortalPetRecordsController"/>, because it has a
/// door of its own: the carer link's "may view history".
/// </summary>
[ApiController]
[Route("portal/pets")]
[RequireScope(Scopes.Portal)]
public sealed class PortalPetsController : ControllerBase
{
    /// <summary><c>GET /portal/pets</c> — "สัตว์ทั้งหมดของบัญชีนี้".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<ListResponse<MyPetDto>>(StatusCodes.Status200OK)]
    public async Task<ListResponse<MyPetDto>> List([FromServices] ListMyPetsHandler handler, CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(cancellationToken));

    /// <summary>
    /// <c>PATCH /portal/pets/{id}</c> — "แก้ได้เฉพาะรูป ชื่อเล่น อาหาร พฤติกรรม". Accepts <c>If-Match</c>.
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="request">Nickname, photo, diet, behaviour — only what changes.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [ProducesResponseType<MyPetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MyPetDto>> Update(
        string id,
        [FromBody] UpdateMyPetRequest request,
        [FromServices] UpdatePetByOwnerHandler handler,
        CancellationToken cancellationToken)
    {
        var pet = await handler.HandleAsync(
            new UpdatePetByOwnerCommand(
                id,
                request.Nickname,
                request.PhotoDocumentId,
                request.Diet,
                request.Behaviour,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(pet.Version);

        return Ok(pet);
    }
}
