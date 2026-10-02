using HappyPaws.Api.Contracts;
using HappyPaws.Application.ServiceCatalog.Dtos;
using HappyPaws.Application.ServiceCatalog.ListBookableServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// The clinic's public service menu — "หมวด ราคา ระยะเวลา และการเปิดให้จองออนไลน์".
/// <para>
/// Public, like the availability feed it leads into, and bounded the same way: only services the
/// clinic has opened for online booking are listed, so the internal catalogue cannot be read off
/// an unauthenticated endpoint.
/// </para>
/// </summary>
[ApiController]
[Route("services")]
[AllowAnonymous]
public sealed class ServicesController : ControllerBase
{
    /// <summary><c>GET /services</c> — "เฉพาะบริการที่เปิดให้จองออนไลน์".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="category">Narrow to one category code.</param>
    /// <param name="branch">Narrow to what one branch offers.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<ListResponse<PublicServiceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResponse<PublicServiceDto>>> List(
        [FromServices] ListBookableServicesHandler handler,
        [FromQuery] string? category,
        [FromQuery] string? branch,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(
            await handler.HandleAsync(new ListBookableServicesQuery(category, branch), cancellationToken)));

    /// <summary>
    /// <c>GET /services/categories</c> — "ตรวจ วัคซีน ทันตกรรม ผ่าตัด แล็บ กรูมมิ่ง".
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    [HttpGet("categories")]
    [ProducesResponseType<ListResponse<ServiceCategoryDto>>(StatusCodes.Status200OK)]
    public ActionResult<ListResponse<ServiceCategoryDto>> Categories(
        [FromServices] ListServiceCategoriesHandler handler) =>
        Ok(ListResponse.Of(handler.Handle()));
}
