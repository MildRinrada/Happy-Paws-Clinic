using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Crm;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Crm.CreatePackage;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Selling packages to customers.</summary>
[ApiController]
[Route("staff/packages")]
public sealed class PackagesController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/packages</c> — "แพ็กเกจตรวจ 4 ครั้ง · ตัดสิทธิ์อัตโนมัติเมื่อใช้". <c>201</c> with the
    /// package and the bill it was sold on. The package is usable once that bill is paid; after that
    /// its uses are spent automatically when a covered service is billed.
    /// <para>
    /// Requires <c>Idempotency-Key</c>: it raises a bill, and a retried sale must not charge twice.
    /// </para>
    /// </summary>
    /// <param name="request">The customer, the services, the number of uses and the price.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [Idempotent]
    [RequireScope(Scopes.Crm.Manage)]
    [ProducesResponseType<PackageSaleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Sell(
        [FromBody] CreatePackageRequest request,
        [FromServices] CreatePackageHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await handler.HandleAsync(request.ToCommand(), cancellationToken));
}
