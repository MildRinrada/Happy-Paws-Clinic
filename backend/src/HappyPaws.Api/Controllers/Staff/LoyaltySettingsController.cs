using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Crm;
using HappyPaws.Api.Http;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Crm.ManageLoyaltySettings;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The points programme, beside the clinic's other settings. Neither endpoint is in the API
/// reference: see <c>docs/contract-extensions.md § 25</c>.
/// <para>
/// On <c>settings:read</c> and <c>settings:write</c>, like every <c>/clinic/settings</c> path: the
/// value of a point and how points are earned are clinic rules, set by whoever sets the clinic's
/// rules, while <c>crm:manage</c> runs promotions inside those rules.
/// </para>
/// </summary>
[ApiController]
[Route("clinic/settings/loyalty")]
public sealed class LoyaltySettingsController : ControllerBase
{
    /// <summary>
    /// <c>GET /clinic/settings/loyalty</c> — the rate, the point value, the level thresholds and the
    /// expiry, with <c>version</c> and an <c>ETag</c>. The defaults at version 0 until saved.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Settings.Read)]
    [ProducesResponseType<LoyaltySettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LoyaltySettingsDto>> Get(
        [FromServices] GetLoyaltySettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }

    /// <summary>
    /// <c>PUT /clinic/settings/loyalty</c> — replaces the programme. Applies from now on: earning,
    /// redeeming and expiry stamps from this moment use the new values, and no ledger row is
    /// rewritten. <c>If-Match</c> honoured. Audited as <c>settings.loyalty_changed</c>.
    /// </summary>
    /// <param name="request">Every value.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut]
    [RequireScope(Scopes.Settings.Write)]
    [ProducesResponseType<LoyaltySettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LoyaltySettingsDto>> Replace(
        [FromBody] ReplaceLoyaltySettingsRequest request,
        [FromServices] ReplaceLoyaltySettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(
            new ReplaceLoyaltySettingsCommand(
                request.SatangPerPoint,
                request.PointValueSatang,
                request.SilverMinPoints,
                request.GoldMinPoints,
                request.PointsExpireAfterMonths,
                IfMatch.Parse(Request.Headers.IfMatch),
                request.ReferralEnabled,
                request.ReferralReferrerPoints,
                request.ReferralFriendPoints,
                request.ReferralMonthlyCap),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }
}
