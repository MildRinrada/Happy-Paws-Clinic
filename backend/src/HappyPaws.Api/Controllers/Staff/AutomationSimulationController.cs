using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Automation;
using HappyPaws.Application.Automation.Dtos;
using HappyPaws.Application.Automation.SimulateAutomation;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>A dry run of a rule.</summary>
[ApiController]
[Route("staff/automations/{id}/simulate")]
public sealed class AutomationSimulationController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/automations/{id}/simulate</c> — "ดูว่าจะส่งถึงใครบ้างก่อนเปิดใช้จริง". Who the
    /// rule would message over the next <c>days</c> (7 by default, at most 30), with the words, and
    /// which of them a run right now would send. Evaluated by the same code as the real run;
    /// nothing is sent. A POST because it is an action on the rule, as the contract has it, though
    /// it changes nothing.
    /// </summary>
    /// <param name="id">The rule.</param>
    /// <param name="request">Optional <c>{ "days": n }</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Automation.Manage)]
    [ProducesResponseType<SimulationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<SimulationDto> Simulate(
        string id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SimulateAutomationRequest? request,
        [FromServices] SimulateAutomationHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SimulateAutomationCommand(id, request?.Days), cancellationToken);
}
