using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.StaffAi;
using HappyPaws.Api.Extensions;
using HappyPaws.Application.Assistant.Chat;
using HappyPaws.Application.StaffAi.Ask;
using HappyPaws.Application.StaffAi.DailyBrief;
using HappyPaws.Application.StaffAi.OwnerMessage;
using HappyPaws.Application.StaffAi.PetSummary;
using HappyPaws.Application.StaffAi.SoapDraft;
using HappyPaws.Application.StaffAi.Usage;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The staff AI assistant: <b>not in the API reference</b>; see <c>docs/contract-extensions.md § 34</c>.
/// <para>
/// Every route that calls the model needs <c>ai:use</c> plus the scope of the data it reads, so the
/// assistant never widens what a person can see. Nothing here saves or sends anything: drafts come
/// back for a person to check and use through the ordinary forms. The deployment's monthly
/// allowance answers <c>429 ai_quota_exceeded</c> once spent; a deployment with no model answers
/// <c>503 ai_unavailable</c>, and <c>GET /staff/ai/usage</c> says <c>enabled: false</c> so the
/// pages hide the buttons.
/// </para>
/// </summary>
[ApiController]
[Route("staff/ai")]
public sealed class StaffAiController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/ai/pets/{id}/summary</c>: one paragraph for the vet before an exam, every fact
    /// cited to a record. Served from the cache, uncounted, while the pet's data is unchanged.
    /// Written to the access log as <c>ai.pet_summary</c>.
    /// </summary>
    /// <param name="id">Pet id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("pets/{id}/summary")]
    [EnableRateLimiting(RateLimitingExtensions.StaffAiPolicy)]
    [RequireScope(Scopes.Record.Read)]
    [RequireScope(Scopes.Ai.Use)]
    [ProducesResponseType<PetSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<PetSummaryDto> PetSummary(
        string id,
        [FromServices] SummarizePetHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(id, cancellationToken);

    /// <summary>
    /// <c>POST /staff/ai/consultations/{id}/soap-draft</c>: the vet's short notes as a draft of the
    /// case's own fields, with <c>warnings</c> for anything in it the notes do not contain. Nothing
    /// is saved. <c>409 state_conflict</c> on a closed case.
    /// </summary>
    /// <param name="id">Consultation id.</param>
    /// <param name="request">The notes.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("consultations/{id}/soap-draft")]
    [EnableRateLimiting(RateLimitingExtensions.StaffAiPolicy)]
    [RequireScope(Scopes.Consult.Write)]
    [RequireScope(Scopes.Ai.Use)]
    [ProducesResponseType<SoapDraftDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<SoapDraftDto> SoapDraft(
        string id,
        [FromBody] SoapDraftRequest request,
        [FromServices] DraftSoapHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new DraftSoapCommand(id, request.Notes), cancellationToken);

    /// <summary>
    /// <c>POST /staff/ai/drafts/owner-message</c>: aftercare instructions from a case, or a reply in
    /// a customer thread, as <c>{ "text": "..." }</c>. Never sends anything. Aftercare also needs
    /// <c>consult:read</c>, a reply <c>message:read</c>, checked in the handler.
    /// </summary>
    /// <param name="request">What to draft from.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("drafts/owner-message")]
    [EnableRateLimiting(RateLimitingExtensions.StaffAiPolicy)]
    [RequireScope(Scopes.Message.Write)]
    [RequireScope(Scopes.Ai.Use)]
    [ProducesResponseType<OwnerMessageDraftDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<OwnerMessageDraftDto> OwnerMessage(
        [FromBody] OwnerMessageDraftRequest request,
        [FromServices] DraftOwnerMessageHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new DraftOwnerMessageCommand(request.Kind, request.ConsultationId, request.ThreadId), cancellationToken);

    /// <summary>
    /// <c>POST /staff/ai/ask</c>: questions about the clinic's numbers, answered from the reports the
    /// caller may read, as the caller. The same shape as <c>POST /assistant/chat</c>. Not stored.
    /// </summary>
    /// <param name="request">The conversation so far.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("ask")]
    [EnableRateLimiting(RateLimitingExtensions.StaffAiPolicy)]
    [RequireScope(Scopes.Ai.Use)]
    [ProducesResponseType<ChatReplyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<ChatReplyDto> Ask(
        [FromBody] AskRequest request,
        [FromServices] AskHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new AskCommand([.. (request.Messages ?? []).Select(message => new ChatMessageInput(message.Role ?? "", message.Text ?? ""))]),
            cancellationToken);

    /// <summary>
    /// <c>GET /staff/ai/daily-brief?branch_id=</c>: today's stored morning brief for the branch.
    /// Always <c>200</c> with <c>status</c> <c>ready</c>, <c>failed</c>, <c>pending</c> or
    /// <c>disabled</c>. Never calls the model and never counts a use.
    /// </summary>
    /// <param name="branchId">The branch.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("daily-brief")]
    [RequireScope(Scopes.Dashboard.Read)]
    [ProducesResponseType<DailyBriefDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<DailyBriefDto> DailyBrief(
        [FromQuery(Name = "branch_id")] string? branchId,
        [FromServices] GetDailyBriefHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(branchId, cancellationToken);

    /// <summary>
    /// <c>GET /staff/ai/usage</c>: <c>{ enabled, month, limit, used, by_feature }</c> for this clinic
    /// month.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("usage")]
    [RequireScope(Scopes.Ai.Use)]
    [ProducesResponseType<StaffAiUsageDto>(StatusCodes.Status200OK)]
    public Task<StaffAiUsageDto> Usage(
        [FromServices] GetStaffAiUsageHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(cancellationToken);
}
