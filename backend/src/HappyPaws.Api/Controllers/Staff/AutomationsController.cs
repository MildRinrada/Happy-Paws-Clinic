using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Automation;
using HappyPaws.Api.Http;
using HappyPaws.Application.Automation.CreateAutomation;
using HappyPaws.Application.Automation.DeleteAutomation;
using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Automation.ListAutomations;
using HappyPaws.Application.Automation.UpdateAutomation;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// "Reminders และ Automation": rule = trigger + condition + action, evaluated by the server.
/// <para>
/// The contract's list and create, and the edit and delete it does not have
/// (<c>docs/contract-extensions.md § 19</c>): without them a rule, the four starter rules among
/// them, could never be switched on or off once made.
/// </para>
/// </summary>
[ApiController]
[Route("staff/automations")]
public sealed class AutomationsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/automations</c> — "กติกาทั้งหมดพร้อมสถิติการยิงย้อนหลัง". A short list, returned
    /// whole in <c>{ "data": [...] }</c>, newest first. A branch-bound reader sees their branches'
    /// rules and the clinic-wide ones.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Automation.Read)]
    [ProducesResponseType<ListResponse<AutomationDto>>(StatusCodes.Status200OK)]
    public async Task<ListResponse<AutomationDto>> List(
        [FromServices] ListAutomationsHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(cancellationToken));

    /// <summary>
    /// <c>POST /staff/automations</c> — "เช่น ครบกำหนดวัคซีน 14 วัน → ส่ง LINE". <c>201</c> with the
    /// rule. Off unless <c>enabled: true</c> is sent.
    /// </summary>
    /// <param name="request">The rule.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Automation.Manage)]
    [ProducesResponseType<AutomationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAutomationRequest request,
        [FromServices] CreateAutomationHandler handler,
        CancellationToken cancellationToken)
    {
        var rule = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, rule);
    }

    /// <summary>
    /// <c>PATCH /staff/automations/{id}</c> — switch a rule on or off, or change its name, timing,
    /// conditions or channel. Not in the API reference; see <c>docs/contract-extensions.md § 19</c>.
    /// The same checks as create; what the rule watches cannot change. <c>If-Match</c> is honoured.
    /// </summary>
    /// <param name="id">Rule id.</param>
    /// <param name="request">The parts to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [RequireScope(Scopes.Automation.Manage)]
    [ProducesResponseType<AutomationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AutomationDto>> Update(
        string id,
        [FromBody] UpdateAutomationRequest request,
        [FromServices] UpdateAutomationHandler handler,
        CancellationToken cancellationToken)
    {
        var rule = await handler.HandleAsync(request.ToCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(rule.Version);

        return Ok(rule);
    }

    /// <summary>
    /// <c>DELETE /staff/automations/{id}</c> — the rule stops and leaves the list; what it sent stays
    /// in the history. Not in the API reference; see <c>docs/contract-extensions.md § 19</c>.
    /// <c>If-Match</c> is honoured.
    /// </summary>
    /// <param name="id">Rule id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{id}")]
    [RequireScope(Scopes.Automation.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        string id,
        [FromServices] DeleteAutomationHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteAutomationCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        return NoContent();
    }
}
