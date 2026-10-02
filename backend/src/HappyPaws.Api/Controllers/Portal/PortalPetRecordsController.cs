using HappyPaws.Api.Authorization;
using HappyPaws.Application.MedicalRecords.GetOwnerVisibleRecords;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>A pet's history as its owner may see it.</summary>
[ApiController]
[Route("portal/pets/{id}/records")]
[RequireScope(Scopes.Portal)]
public sealed class PortalPetRecordsController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/pets/{id}/records</c> — "เฉพาะส่วนที่คลินิกตั้งค่าให้เจ้าของเห็น".
    /// <para>
    /// Only the sections the clinic opened in <c>owner_visible_sections</c>; the rest are absent.
    /// <c>404</c> unless the caller cares for the pet with history access.
    /// </para>
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<OwnerRecordsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OwnerRecordsDto> Get(
        string id,
        [FromServices] GetOwnerVisibleRecordsHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetOwnerVisibleRecordsQuery(id), cancellationToken);
}
