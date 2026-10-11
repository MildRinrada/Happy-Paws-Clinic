using HappyPaws.Api.Contracts;
using HappyPaws.Api.Http;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Clinics.GetOpeningHours;
using HappyPaws.Application.Clinics.BrandImages;
using HappyPaws.Application.Clinics.ListBranches;
using HappyPaws.Application.Clinics.ManageContacts;
using HappyPaws.Domain.Clinics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// What the clinic tells the world about itself — "อ่านได้แบบ public · แก้ได้เฉพาะเจ้าของและแอดมิน".
/// <para>
/// The three reads the contract marks public. They are what the clinic's own website renders
/// before anyone signs in, so <c>[AllowAnonymous]</c> opts them out of the fallback policy. The
/// writes on the same paths live in <c>Staff/ClinicSetupController</c> behind
/// <c>settings:write</c>.
/// </para>
/// </summary>
[ApiController]
[Route("clinic")]
[AllowAnonymous]
public sealed class ClinicController : ControllerBase
{
    /// <summary><c>GET /clinic</c> — "ชื่อ โลโก้ สี ใบอนุญาต ผู้ควบคุมกิจการ".</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<ClinicProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ClinicProfileDto>> Get(
        [FromServices] GetClinicProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var profile = await handler.HandleAsync(cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(profile.Version);

        return Ok(profile);
    }

    /// <summary>
    /// <c>GET /clinic/logo</c> — <b>not in the API reference</b>; see
    /// <c>docs/contract-extensions.md § 50</c>. A <c>302</c> to a signed link to the clinic's
    /// uploaded logo, minted for this request. The address never changes, so pages, a phone's home
    /// screen and structured data can keep it; the redirect may be reused for five minutes.
    /// <c>404</c> when the clinic has no uploaded logo.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("logo")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Logo([FromServices] GetBrandImageHandler handler, CancellationToken cancellationToken) =>
        RedirectToImageAsync(ClinicBrandImageKind.Logo, handler, cancellationToken);

    /// <summary>
    /// <c>GET /clinic/tab-icon</c> — the same for the optional square tab icon. <c>404</c> when
    /// none was uploaded; it does not fall back to the logo. <b>Contract extension</b>, § 50.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("tab-icon")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> TabIcon([FromServices] GetBrandImageHandler handler, CancellationToken cancellationToken) =>
        RedirectToImageAsync(ClinicBrandImageKind.TabIcon, handler, cancellationToken);

    private async Task<IActionResult> RedirectToImageAsync(
        ClinicBrandImageKind kind,
        GetBrandImageHandler handler,
        CancellationToken cancellationToken)
    {
        var link = await handler.HandleAsync(kind, cancellationToken);

        Response.Headers.CacheControl = $"public, max-age={(int)BrandImageLinks.RedirectCacheLifetime.TotalSeconds}";

        return Redirect(link);
    }

    /// <summary><c>GET /clinic/branches</c> — "สาขา ที่อยู่ พิกัด ห้องตรวจ". Open branches only.</summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("branches")]
    [ProducesResponseType<ListResponse<BranchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResponse<BranchDto>>> Branches(
        [FromServices] ListBranchesHandler handler,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(await handler.HandleAsync(cancellationToken)));

    /// <summary>
    /// <c>GET /clinic/contacts</c> — <b>not in the API reference</b>; see
    /// <c>docs/contract-extensions.md § 15</c>. The phone numbers, email, chat accounts and maps the
    /// website shows, in display order. Written in <c>Staff/ClinicContactsController</c>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("contacts")]
    [ProducesResponseType<ListResponse<ContactDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResponse<ContactDto>>> Contacts(
        [FromServices] ListContactsHandler handler,
        CancellationToken cancellationToken) =>
        Ok(ListResponse.Of(await handler.HandleAsync(cancellationToken)));

    /// <summary>
    /// <c>GET /clinic/hours</c> — "เวลาทำการรายวัน วันหยุดประจำสัปดาห์", with the special holidays
    /// still to come.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("hours")]
    [ProducesResponseType<OpeningHoursDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OpeningHoursDto>> Hours(
        [FromServices] GetOpeningHoursHandler handler,
        CancellationToken cancellationToken)
    {
        var hours = await handler.HandleAsync(cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(hours.Version);

        return Ok(hours);
    }
}
