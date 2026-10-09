using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Crm;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Crm.CreatePackage;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Crm.ListPackages;
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

    /// <summary>
    /// <c>GET /staff/packages?customer_id=&amp;status=&amp;branch=</c> — one customer's packages in
    /// <c>{ "data": [...] }</c>, usable first. Clinic-wide like the loyalty account; <c>branch</c> only
    /// narrows to packages sold there. Not in the API reference (<c>docs/contract-extensions.md § 25</c>).
    /// </summary>
    /// <param name="customerId">Required.</param>
    /// <param name="status"><c>pending_payment</c>, <c>active</c>, <c>used_up</c>, <c>expired</c> or <c>cancelled</c>.</param>
    /// <param name="branch">Only packages sold at this branch.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Crm.Read)]
    [ProducesResponseType<ListResponse<PackageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ListResponse<PackageDto>> List(
        [FromQuery(Name = "customer_id")] string? customerId,
        [FromQuery] string? status,
        [FromQuery] string? branch,
        [FromServices] ListPackagesHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(new ListPackagesQuery(customerId, status, branch), cancellationToken));
}
