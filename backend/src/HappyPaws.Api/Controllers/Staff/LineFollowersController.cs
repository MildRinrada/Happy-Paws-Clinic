using HappyPaws.Api.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Integrations.ListLineFollowers;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The recent followers of the clinic's LINE account, for the counter to pick a customer's LINE
/// address from. Not in the API reference; see <c>docs/contract-extensions.md § 18</c>.
/// <para>
/// <b><c>customer:write</c>, not <c>integration:manage</c> or <c>customer:read</c>.</b> The list
/// exists only to fill in a customer's LINE address, which is a write to the customer's contact
/// details; whoever may do that needs to see it, and nobody else does. The names and pictures of
/// people who are not yet customers are not shown to every role that can read the register.
/// </para>
/// </summary>
[ApiController]
[Route("staff/integrations/line/followers")]
[RequireScope(Scopes.Customer.Write)]
public sealed class LineFollowersController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/integrations/line/followers?unlinked=true&amp;q=&amp;limit=&amp;cursor=</c> — who
    /// followed or wrote in the last 14 days, most recent first. <c>unlinked</c> is true unless sent
    /// as false; <c>q</c> searches the LINE display name.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="unlinked">Leave out addresses already on a customer's record. Default true.</param>
    /// <param name="q">Part of the LINE display name.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<CursorPage<LineFollowerDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<LineFollowerDto>> List(
        [FromServices] ListLineFollowersHandler handler,
        [FromQuery] bool? unlinked,
        [FromQuery] string? q,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListLineFollowersQuery(
                unlinked ?? true,
                q,
                new CursorPageRequest(limit is 0 ? CursorPaging.DefaultLimit : limit, cursor)),
            cancellationToken);
}
