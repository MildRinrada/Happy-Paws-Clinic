using HappyPaws.Api.Authorization;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Application.Queueing.GetMyTicket;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// <c>GET /portal/queue/{id}</c> — "เลขคิวตัวเอง · กำลังเรียก · เหลืออีกกี่คิว".
/// </summary>
[ApiController]
[Route("portal/queue")]
[RequireScope(Scopes.Portal)]
public sealed class PortalQueueController : ControllerBase
{
    /// <summary>
    /// The owner's own place in line. Someone else's ticket answers <c>404</c>, not <c>403</c> —
    /// confirming which ids exist would expose how busy the clinic is and who is in it.
    /// </summary>
    /// <param name="id">Queue ticket id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [ProducesResponseType<MyQueueTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyQueueTicketDto>> Get(
        string id,
        [FromServices] GetMyTicketHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new GetMyTicketQuery(id), cancellationToken));
}
