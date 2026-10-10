using HappyPaws.Application.PrivacyPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// The clinic's privacy notice, as published. <b>Contract extension:</b> <c>API Reference.dc.html</c>
/// has nothing like it; see <c>docs/contract-extensions.md § 35</c>.
/// <para>
/// Public and read-only, and only ever published text: a draft, the seeded template included, is
/// never served here. Both reads may be cached for a minute, so a newly published version reaches
/// the page within about that long.
/// </para>
/// </summary>
[ApiController]
[Route("privacy-policy")]
[AllowAnonymous]
public sealed class PrivacyPolicyController : ControllerBase
{
    private const string ShortPublicCache = "public, max-age=60";

    /// <summary>
    /// <c>GET /privacy-policy</c>: the current notice. <c>200</c> with <c>published: false</c> when the
    /// clinic has not published one yet. Contract extension.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<PublicPrivacyPolicyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicPrivacyPolicyDto>> Current(
        [FromServices] GetPublicPrivacyPolicyHandler handler,
        CancellationToken cancellationToken)
    {
        var policy = await handler.CurrentAsync(cancellationToken);
        Response.Headers.CacheControl = ShortPublicCache;

        return Ok(policy);
    }

    /// <summary>
    /// <c>GET /privacy-policy/versions/{label}</c>: a published version by its label, current or
    /// older, so the clinic can show what a customer agreed to. A draft is <c>404</c>. Contract extension.
    /// </summary>
    /// <param name="label">The version's label, <c>2026-11</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("versions/{label}")]
    [ProducesResponseType<PublicPrivacyPolicyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicPrivacyPolicyDto>> Version(
        string label,
        [FromServices] GetPublicPrivacyPolicyHandler handler,
        CancellationToken cancellationToken)
    {
        var policy = await handler.ByLabelAsync(label, cancellationToken);
        Response.Headers.CacheControl = ShortPublicCache;

        return Ok(policy);
    }
}
