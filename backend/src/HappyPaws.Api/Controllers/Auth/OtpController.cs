using HappyPaws.Api.Contracts.Identity;
using HappyPaws.Application.Identity.RequestOtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Auth;

/// <summary>One-time codes by SMS.</summary>
[ApiController]
[Route("auth/otp")]
[AllowAnonymous]
public sealed class OtpController : ControllerBase
{
    /// <summary>
    /// <c>POST /auth/otp/request</c> — "rate limit 3 ครั้ง/เบอร์/10 นาที".
    /// <para>
    /// <c>202</c> with when the code expires, the same whether or not the number has an account.
    /// Over the limit is <c>429 rate_limited</c>.
    /// </para>
    /// </summary>
    /// <param name="request">The phone and what the code is for.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("request")]
    [ProducesResponseType<OtpRequestedDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Send(
        [FromBody] RequestOtpRequest request,
        [FromServices] RequestOtpHandler handler,
        CancellationToken cancellationToken) =>
        Accepted(await handler.HandleAsync(new RequestOtpCommand(request.Phone, request.Purpose), cancellationToken));
}
