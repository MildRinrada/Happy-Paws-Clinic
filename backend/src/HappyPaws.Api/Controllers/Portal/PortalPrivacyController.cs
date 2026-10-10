using HappyPaws.Api.Contracts.Customers;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Customers.DataRequests;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Application.Customers.GetConsents;
using HappyPaws.Application.Customers.RecordConsent;
using HappyPaws.Domain.Customers;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// What a customer can see and change about their own data.
/// <para>
/// <b>None of these routes are in the API reference.</b> The contract has no consent, retention or
/// erasure endpoint anywhere, while the system it describes holds identifiable personal data and
/// animal health data in Thailand. See <c>docs/contract-extensions.md § 4</c>.
/// </para>
/// <para>
/// The customer id is taken from the token, never from the request. A portal route that accepted
/// one would let any signed-in customer read or change anyone else's consent.
/// </para>
/// </summary>
[ApiController]
[Route("portal")]
public sealed class PortalPrivacyController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/consents</c> — where this customer stands, and how they got there.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="currentUser">The signed-in customer.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("consents")]
    [ProducesResponseType<ConsentSummaryDto>(StatusCodes.Status200OK)]
    public Task<ConsentSummaryDto> Consents(
        [FromServices] GetConsentsHandler handler,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetConsentsQuery(currentUser.RequireCustomerId()), cancellationToken);

    /// <summary>
    /// <c>PUT /portal/consents</c>: the customer changes their own mind. Each new row carries the
    /// label of the privacy notice current now; <c>policy_version</c> in the body is accepted for
    /// older clients and ignored (<c>docs/contract-extensions.md § 35</c>).
    /// </summary>
    /// <param name="request">Purpose to position. Omitted purposes are left alone.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="currentUser">The signed-in customer.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut("consents")]
    [ProducesResponseType<ConsentSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ConsentSummaryDto> UpdateConsents(
        [FromBody] UpdateConsentsRequest request,
        [FromServices] RecordConsentHandler handler,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new RecordConsentCommand(
                currentUser.RequireCustomerId(),
                request.Consents,
                ConsentSource.Portal),
            cancellationToken);

    /// <summary>
    /// <c>POST /portal/data-requests</c> — exercise a data-protection right.
    /// <para>
    /// This records and times the request; it does not act on it. An erasure request against a
    /// signed medical record collides with a retention duty, and reconciling the two is a judgement
    /// a person has to make and be accountable for.
    /// </para>
    /// </summary>
    /// <param name="request">The right being exercised, and what the customer is asking for.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="currentUser">The signed-in customer.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("data-requests")]
    [ProducesResponseType<DataRequestDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DataRequestDto>> RaiseDataRequest(
        [FromBody] RaiseDataRequestBody request,
        [FromServices] RaiseDataRequestHandler handler,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var raised = await handler.HandleAsync(
            new RaiseDataRequestCommand(currentUser.RequireCustomerId(), request.Type, request.Detail),
            cancellationToken);

        return Created($"/staff/data-requests/{raised.Id}", raised);
    }
}
