using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Automation;
using HappyPaws.Application.Automation.CreateAutomation;
using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Automation.ListAutomations;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// "Reminders และ Automation": rule = trigger + condition + action, evaluated by the server.
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
}
