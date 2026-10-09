using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Crm;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Crm.AdjustPoints;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Crm.GetLoyaltyAccount;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>A customer's points, level and history.</summary>
[ApiController]
[Route("staff/loyalty")]
public sealed class LoyaltyController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/loyalty/{customer_id}</c> — "แต้มสะสม ระดับสมาชิก และประวัติแลก". Also lists the
    /// customer's packages, which staff have no other way to read.
    /// </summary>
    /// <param name="customerId">The customer.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{customerId}")]
    [RequireScope(Scopes.Crm.Read)]
    [ProducesResponseType<LoyaltyAccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<LoyaltyAccountDto> Get(
        string customerId,
        [FromServices] GetLoyaltyAccountHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(customerId, cancellationToken);

    /// <summary>
    /// <c>POST /staff/loyalty/{customer_id}/adjustments</c> — gives (<c>points</c> above zero) or takes
    /// (below zero) points by hand with a required <c>reason</c>. <c>201</c> with the ledger row and the
    /// balance after. Taking more than the balance is <c>409 insufficient_points</c>. Moves the balance,
    /// not the level. Requires <c>Idempotency-Key</c>. Audited as <c>loyalty.adjusted</c>. Not in the
    /// API reference (<c>docs/contract-extensions.md § 25</c>).
    /// </summary>
    /// <param name="customerId">The customer.</param>
    /// <param name="request">The signed points and the reason.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{customerId}/adjustments")]
    [Idempotent]
    [RequireScope(Scopes.Crm.Manage)]
    [ProducesResponseType<LoyaltyAdjustmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Adjust(
        string customerId,
        [FromBody] AdjustPointsRequest request,
        [FromServices] AdjustPointsHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(new AdjustPointsCommand(customerId, request.Points, request.Reason), cancellationToken));
}
