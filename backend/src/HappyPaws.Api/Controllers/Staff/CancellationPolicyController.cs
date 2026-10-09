using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Api.Http;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointmentRules;
using HappyPaws.Application.Appointments.ReplaceCancellationPolicy;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The cancellation policy. Only <c>PUT</c> is in the contract; the <c>GET</c> is
/// <c>docs/contract-extensions.md § 23</c>.
/// </summary>
[ApiController]
[Route("staff/policies/cancellation")]
public sealed class CancellationPolicyController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/policies/cancellation</c> — the policy as stored, with <c>version</c> and an
    /// <c>ETag</c> to send back as <c>If-Match</c>. Not in the API reference (§ 23). Read on
    /// <c>settings:read</c>, the read side of the <c>settings:write</c> the <c>PUT</c> needs, as
    /// <c>GET /clinic/settings/booking</c> is.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Settings.Read)]
    [ProducesResponseType<CancellationPolicyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CancellationPolicyDto>> Get(
        [FromServices] GetCancellationPolicyHandler handler,
        CancellationToken cancellationToken)
    {
        var policy = await handler.HandleAsync(cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(policy.Version);

        return Ok(policy);
    }

    /// <summary>
    /// <c>PUT /staff/policies/cancellation</c> — "cutoff ค่าปรับ และจำนวนครั้งที่ยอมให้".
    /// <para>
    /// A customer cancellation inside <c>cutoff_hours</c> is marked late; late cancellations and
    /// no-shows within <c>window_days</c> are strikes, and each past <c>free_allowance</c> carries
    /// <c>fee_satang</c>. The fee is reported, never charged automatically. Audited.
    /// </para>
    /// </summary>
    /// <param name="request">The whole policy.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut]
    [RequireScope(Scopes.Settings.Write)]
    [ProducesResponseType<CancellationPolicyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CancellationPolicyDto>> Replace(
        [FromBody] ReplaceCancellationPolicyRequest request,
        [FromServices] ReplaceCancellationPolicyHandler handler,
        CancellationToken cancellationToken)
    {
        var policy = await handler.HandleAsync(
            new ReplaceCancellationPolicyCommand(
                request.CutoffHours,
                request.FeeSatang,
                request.FreeAllowance,
                request.WindowDays,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(policy.Version);

        return Ok(policy);
    }
}
