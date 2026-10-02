using HappyPaws.Api.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Application.Patients.GetPetHistory;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// A patient's clinical timeline.
/// <para>
/// Its own controller because it is the one patient route that carries <c>record:read</c> rather
/// than <c>patient:read</c> — and because, like <c>GET /staff/medical-records/{id}</c>, every call
/// writes an access-log row. Both facts follow from the same line: the registry entry is an
/// identity card, this is the file.
/// </para>
/// </summary>
[ApiController]
[Route("staff/pets/{id}/history")]
public sealed class PetHistoryController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/pets/{id}/history</c> — "ไทม์ไลน์รวมทุกเคส วัคซีน ยา แล็บ".
    /// <para>
    /// This read is audited: it writes <c>pet.history_viewed</c> before returning, readable through
    /// <c>GET /staff/audit-logs/access</c>.
    /// </para>
    /// <para>
    /// Four kinds of entry: <c>visit</c>, <c>medicine</c>, <c>vaccination</c> and <c>lab</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Record.Read)]
    [ProducesResponseType<CursorPage<PetTimelineEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CursorPage<PetTimelineEntryDto>> Get(
        string id,
        [FromServices] GetPetHistoryHandler handler,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new GetPetHistoryQuery(id, limit is 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);
}
