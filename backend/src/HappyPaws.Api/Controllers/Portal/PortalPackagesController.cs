using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Application.Crm.GetMyPackageBalance;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>The customer's packages.</summary>
[ApiController]
[Route("portal/packages")]
[RequireScope(Scopes.Portal)]
public sealed class PortalPackagesController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/packages</c> — "สิทธิ์คงเหลือของลูกค้าและวันหมดอายุ". Empty until packages are
    /// sold through the Tier-3 loyalty module.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<ListResponse<PackageBalanceDto>>(StatusCodes.Status200OK)]
    public async Task<ListResponse<PackageBalanceDto>> List(
        [FromServices] GetMyPackageBalanceHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(cancellationToken));
}
