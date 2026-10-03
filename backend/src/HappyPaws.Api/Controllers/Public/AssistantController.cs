using HappyPaws.Api.Contracts.Assistant;
using HappyPaws.Api.Extensions;
using HappyPaws.Application.Assistant.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// The website assistant — <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 16</c>.
/// <para>
/// Public, like the pages it answers questions about, and it can tell a visitor nothing those pages
/// would not: its tools are the public reads. Rate-limited per visitor and capped per day, because
/// every message is a call to a paid model.
/// </para>
/// </summary>
[ApiController]
[Route("assistant")]
[AllowAnonymous]
public sealed class AssistantController : ControllerBase
{
    /// <summary><c>GET /assistant</c> — whether the website should show the chat button.</summary>
    /// <param name="handler">Resolved per request.</param>
    [HttpGet]
    [ProducesResponseType<AssistantStatusDto>(StatusCodes.Status200OK)]
    public AssistantStatusDto Status([FromServices] GetAssistantStatusHandler handler) => handler.Handle();

    /// <summary>
    /// <c>POST /assistant/chat</c> — the next reply. Nothing is stored: the widget sends the
    /// conversation each time.
    /// </summary>
    /// <param name="request">The conversation so far.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("chat")]
    [EnableRateLimiting(RateLimitingExtensions.AssistantPolicy)]
    [ProducesResponseType<ChatReplyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<ChatReplyDto> Chat(
        [FromBody] ChatRequest request,
        [FromServices] ChatHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ChatCommand([.. (request.Messages ?? []).Select(message => new ChatMessageInput(message.Role ?? "", message.Text ?? ""))]),
            cancellationToken);
}
