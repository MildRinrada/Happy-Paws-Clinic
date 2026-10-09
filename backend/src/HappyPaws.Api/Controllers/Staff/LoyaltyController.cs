using HappyPaws.Api.Authorization;
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
}
