using HappyPaws.Api.Contracts.Appointments;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Http;
using HappyPaws.Application.Appointments.GuestBooking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Public;

/// <summary>
/// Booking without signing up first. <b>Not in the API reference</b>: see
/// <c>docs/contract-extensions.md § 27</c>.
/// <para>
/// Two steps, both open to anyone. <c>start</c> holds the time and texts a code; <c>verify</c>
/// proves the number, opens or finds the customer, and signs them in the way <c>/auth/login</c>
/// does. The booking itself is then the ordinary <c>POST /portal/appointments</c>, so there is one
/// booking path, with one idempotency rule, whoever the customer was a minute ago.
/// </para>
/// </summary>
[ApiController]
[Route("booking/guest")]
[AllowAnonymous]
public sealed class GuestBookingController(IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// <c>POST /booking/guest/start</c> — holds the time for ten minutes and sends a code to the
    /// phone. The answer is the same whatever the number belongs to. Over the per-number code limit,
    /// or the per-address hold cap, is <c>429 rate_limited</c>.
    /// </summary>
    /// <param name="request">The service, time, optional vet and branch, and the phone.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("start")]
    [EnableRateLimiting(RateLimitingExtensions.GuestHoldPolicy)]
    [ProducesResponseType<GuestBookingStartedDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<GuestBookingStartedDto>> Start(
        [FromBody] StartGuestBookingRequest request,
        [FromServices] StartGuestBookingHandler handler,
        CancellationToken cancellationToken)
    {
        var started = await handler.HandleAsync(
            new StartGuestBookingCommand(request.Phone, request.ServiceId, request.StartsAt, request.StaffId, request.BranchId),
            cancellationToken);

        return Created($"/booking/holds/{started.Hold.Id}", started);
    }

    /// <summary>
    /// <c>POST /booking/guest/verify</c> — proves the number with its code, opens or finds the
    /// customer, hands them the hold, and signs them in: access token in the body, refresh token in
    /// the cookie, as at sign-in. A staff number is <c>409 contact_taken</c>.
    /// </summary>
    /// <param name="request">The hold, the phone and its code, a name, and the privacy notice.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("verify")]
    [EnableRateLimiting(RateLimitingExtensions.GuestVerifyPolicy)]
    [ProducesResponseType<GuestBookingVerifiedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<GuestBookingVerifiedResponse>> Verify(
        [FromBody] VerifyGuestBookingRequest request,
        [FromServices] VerifyGuestBookingHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new VerifyGuestBookingCommand(
                request.HoldId,
                request.Phone,
                request.Code,
                request.DisplayName,
                request.AcceptPrivacyNotice,
                request.PolicyVersion,
                request.MarketingConsent,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                request.ReferralCode),
            cancellationToken);

        RefreshTokenCookie.Write(
            Response,
            result.Tokens.RefreshToken,
            DateTimeOffset.UtcNow.Add(Domain.Identity.UserSession.Lifetime),
            secure: !environment.IsDevelopment());

        return Ok(GuestBookingVerifiedResponse.From(result));
    }
}
