using HappyPaws.Api.Contracts;
using HappyPaws.Api.Http;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Clinics.GetOpeningHours;
using HappyPaws.Application.Clinics.ListBranches;
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
