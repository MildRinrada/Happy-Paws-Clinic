using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.PrivacyPolicies;
using HappyPaws.Api.Http;
using HappyPaws.Application.PrivacyPolicies;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.PrivacyPolicies;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Drafting and publishing the clinic's privacy notice. <b>Contract extension:</b>
/// <c>API Reference.dc.html</c> has nothing like it; see <c>docs/contract-extensions.md § 35</c>.
/// <para>
/// The clinic is the data controller and writes its own notice. Reading is <c>settings:read</c>,
/// drafting <c>settings:write</c>. Publishing makes a text the clinic's legal statement to every
/// customer and fixes it for good, so it is <c>settings:danger</c>: the owner, not
/// <c>clinic_admin</c>, and only after 2FA.
/// </para>
/// </summary>
[ApiController]
[Route("staff/privacy-policy/versions")]
public sealed class StaffPrivacyPolicyController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/privacy-policy/versions</c>: every version with its text, drafts included,
    /// newest made first, which one is current, and whether a newer template has shipped.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Settings.Read)]
    [ProducesResponseType<StaffPrivacyPolicyListDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StaffPrivacyPolicyListDto>> List(
        [FromServices] ListPrivacyPolicyVersionsHandler handler,
        CancellationToken cancellationToken)
    {
        var list = await handler.HandleAsync(cancellationToken);
        Response.Headers.CacheControl = "no-store";

        return Ok(list);
    }

    /// <summary>
    /// <c>POST /staff/privacy-policy/versions</c>: a new draft from the template, the current
    /// notice, or any version. Nothing is public until it is published. Audited as
    /// <c>privacy_policy.draft_created</c>.
    /// </summary>
    /// <param name="request">Where the text comes from.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Settings.Write)]
    [ProducesResponseType<StaffPrivacyPolicyVersionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StaffPrivacyPolicyVersionDto>> Create(
        [FromBody] CreatePrivacyPolicyDraftRequest request,
        [FromServices] CreatePrivacyPolicyDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var draft = await handler.HandleAsync(
            new CreatePrivacyPolicyDraftCommand(request.Source, request.VersionId),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(draft.Version);

        return Created("/staff/privacy-policy/versions", draft);
    }

    /// <summary>
    /// <c>PATCH /staff/privacy-policy/versions/{id}</c>: rewrites a draft's sections. A published
    /// version is <c>409 state_conflict</c>. <c>If-Match</c> is required. Audited as
    /// <c>privacy_policy.draft_updated</c>.
    /// </summary>
    /// <param name="id">The draft.</param>
    /// <param name="request">Every section, in order.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [RequireScope(Scopes.Settings.Write)]
    [ProducesResponseType<StaffPrivacyPolicyVersionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StaffPrivacyPolicyVersionDto>> Update(
        string id,
        [FromBody] UpdatePrivacyPolicyDraftRequest request,
        [FromServices] UpdatePrivacyPolicyDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var sections = request.Sections?
            .Select(section => new PrivacyPolicySection(
                section?.Anchor ?? string.Empty,
                section?.Heading ?? string.Empty,
                section?.Body ?? string.Empty))
            .ToList();

        var draft = await handler.HandleAsync(
            new UpdatePrivacyPolicyDraftCommand(id, sections, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(draft.Version);

        return Ok(draft);
    }

    /// <summary>
    /// <c>POST /staff/privacy-policy/versions/{id}/publish</c>: makes a draft the clinic's notice,
    /// current from now, with its label and effective date. Owner only (<c>settings:danger</c>).
    /// <c>If-Match</c> is required. Audited as <c>privacy_policy.published</c>.
    /// </summary>
    /// <param name="id">The draft.</param>
    /// <param name="request">Label, effective date, and whether a lawyer reviewed it.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/publish")]
    [RequireScope(Scopes.Settings.Danger)]
    [ProducesResponseType<StaffPrivacyPolicyVersionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StaffPrivacyPolicyVersionDto>> Publish(
        string id,
        [FromBody] PublishPrivacyPolicyRequest request,
        [FromServices] PublishPrivacyPolicyHandler handler,
        CancellationToken cancellationToken)
    {
        var published = await handler.HandleAsync(
            new PublishPrivacyPolicyCommand(
                id,
                request.Label,
                request.EffectiveDate,
                request.LegalReviewDone,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(published.Version);

        return Ok(published);
    }
}
