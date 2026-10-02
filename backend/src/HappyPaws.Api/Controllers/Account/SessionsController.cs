using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.ManageSessions;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Account;

/// <summary>
/// The devices this account is signed in on — "อุปกรณ์ที่ล็อกอินอยู่ · เพิกถอนรายตัวได้".
/// </summary>
[ApiController]
[Route("me/sessions")]
public sealed class SessionsController : ControllerBase
{
    /// <summary><c>GET /me/sessions</c></summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SessionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SessionDto>>> List(
        [FromServices] ListSessionsHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));

    /// <summary><c>DELETE /me/sessions/{id}</c> — kicks a lost device off.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        string id,
        [FromServices] RevokeSessionHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
