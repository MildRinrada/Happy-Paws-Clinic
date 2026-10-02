using HappyPaws.Api.Contracts;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Application.Staff.ListTeam;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// The clinic's people as the public website introduces them, grouped by department.
/// <b>Contract extension:</b> the contract has no public staff list. See
/// <c>docs/contract-extensions.md § 10</c>.
/// <para>
/// Lists only members whose public profile says to, and only what that profile holds: no email,
/// phone, role, branch or licence ever leaves through here.
/// </para>
/// </summary>
[ApiController]
[Route("team")]
[AllowAnonymous]
public sealed class TeamController : ControllerBase
{
    /// <summary><c>GET /team</c>. Departments in page order, each with its listed members.</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="department">Only this department, e.g. <c>veterinary</c>.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<ListResponse<TeamDepartmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ListResponse<TeamDepartmentDto>>> List(
        [FromServices] ListTeamHandler handler,
        [FromQuery] string? department,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(await handler.HandleAsync(new ListTeamQuery(department), cancellationToken)));
}
