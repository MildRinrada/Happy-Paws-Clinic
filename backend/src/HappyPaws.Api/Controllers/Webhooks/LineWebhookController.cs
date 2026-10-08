using HappyPaws.Api.Errors;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Integrations.HandleLineWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Webhooks;

/// <summary>
/// LINE's callback for the clinic's Official Account.
/// <para>
/// <b>Anonymous by bearer token, authenticated by signature.</b> LINE sends no token; it signs the
/// raw body with the channel secret in <c>X-Line-Signature</c>, and that is checked before anything
/// in the body is read. A request that fails the check gets <c>401</c> and nothing else happens.
/// Nothing here issues a session or links an account: see <c>docs/assumptions.md § A</c>.
/// </para>
/// <para>
/// The body is read raw, never through model binding, because the signature is over the exact
/// bytes LINE sent and a re-serialised object would not match them.
/// </para>
/// </summary>
[ApiController]
[Route("webhooks/line")]
[AllowAnonymous]
public sealed class LineWebhookController : ControllerBase
{
    /// <summary>LINE's webhook bodies are small; a megabyte is far beyond any real batch.</summary>
    private const long BodyLimit = 1024 * 1024;

    /// <summary>
    /// <c>POST /webhooks/line</c> — "รับ follow · message · postback จากปุ่มในแชต". <c>200</c> once the
    /// signature checks out, whatever each event led to, so LINE does not retry a batch that was
    /// read.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequestSizeLimit(BodyLimit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(
        [FromServices] HandleLineWebhookHandler handler,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);

        var outcome = await handler.HandleAsync(
            new HandleLineWebhookCommand(buffer.ToArray(), Request.Headers["X-Line-Signature"].ToString()),
            cancellationToken);

        switch (outcome)
        {
            case LineWebhookOutcome.BadSignature:
                await ApiErrorWriter.WriteAsync(
                    HttpContext,
                    HttpStatus.Unauthorized,
                    ErrorCodes.Unauthenticated,
                    "ลายเซ็นของ LINE ไม่ถูกต้อง",
                    messageEn: "The LINE signature does not match.");
                return new EmptyResult();

            case LineWebhookOutcome.Malformed:
                await ApiErrorWriter.WriteAsync(
                    HttpContext,
                    HttpStatus.BadRequest,
                    ErrorCodes.ValidationFailed,
                    "รูปแบบข้อมูลจาก LINE ไม่ถูกต้อง",
                    messageEn: "The webhook body is not a LINE event batch.");
                return new EmptyResult();

            default:
                return Ok();
        }
    }
}
