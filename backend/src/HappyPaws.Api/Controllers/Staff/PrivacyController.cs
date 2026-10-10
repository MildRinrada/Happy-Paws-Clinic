using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Customers;
using HappyPaws.Api.Http;
using HappyPaws.Application.Customers.DataRequests;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Application.Customers.GetConsents;
using HappyPaws.Application.Customers.RecordConsent;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Customers;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The clinic's side of consent and data-subject rights.
/// <para>
/// <b>None of these routes are in the API reference</b> — see
/// <c>docs/contract-extensions.md § 4</c>. They exist because a clinic must be able to record a
/// consent given on paper at the counter, and must be able to show that a request to exercise a
/// data right was received, timed and answered.
/// </para>
/// </summary>
[ApiController]
public sealed class PrivacyController : ControllerBase
{
    /// <summary><c>GET /staff/customers/{id}/consents</c> — the customer's current position and history.</summary>
    /// <param name="id">Customer id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("staff/customers/{id}/consents")]
    [RequireScope(Scopes.Customer.Read)]
    [ProducesResponseType<ConsentSummaryDto>(StatusCodes.Status200OK)]
    public Task<ConsentSummaryDto> Consents(
        string id,
        [FromServices] GetConsentsHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetConsentsQuery(id), cancellationToken);

    /// <summary>
    /// <c>POST /staff/customers/{id}/consents</c> — record a decision taken on paper or in person.
    /// <para>
    /// The recording staff account is stored on the row. A consent recorded on someone's behalf
    /// with no record of who recorded it is not evidence of anything.
    /// </para>
    /// <para>
    /// The row carries the label of the privacy notice current now; <c>policy_version</c> in the
    /// body is accepted and ignored (<c>docs/contract-extensions.md § 35</c>).
    /// </para>
    /// </summary>
    /// <param name="id">Customer id.</param>
    /// <param name="request">Purpose to position, with the paper form's reference in <c>note</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("staff/customers/{id}/consents")]
    [RequireScope(Scopes.Customer.Write)]
    [ProducesResponseType<ConsentSummaryDto>(StatusCodes.Status200OK)]
    public Task<ConsentSummaryDto> RecordConsent(
        string id,
        [FromBody] RecordConsentRequest request,
        [FromServices] RecordConsentHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new RecordConsentCommand(
                id, request.Consents, ConsentSource.Counter, Note: request.Note),
            cancellationToken);

    /// <summary>
    /// <c>GET /staff/data-requests?status=open</c> — the clinic's worklist, oldest first.
    /// <para>
    /// Oldest first, unlike every other list here: the top of this one is the request closest to a
    /// statutory deadline.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="status">Narrow to <c>open</c>, <c>completed</c> or <c>refused</c>.</param>
    /// <param name="customerId">Narrow to one customer.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("staff/data-requests")]
    [RequireScope(Scopes.Customer.Read)]
    [ProducesResponseType<IReadOnlyList<DataRequestDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<DataRequestDto>> DataRequests(
        [FromServices] ListDataRequestsHandler handler,
        [FromQuery] string? status,
        [FromQuery(Name = "customer_id")] string? customerId,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ListDataRequestsQuery(status, customerId), cancellationToken);

    /// <summary>
    /// <c>POST /staff/data-requests/{id}/close</c> — what the clinic did, or why it declined.
    /// <para>
    /// Refusing is a first-class outcome. An erasure request against a signed medical record will
    /// often be refused, and the lawful response is an answer with a stated reason, not silence.
    /// </para>
    /// </summary>
    /// <param name="id">Data request id.</param>
    /// <param name="request">The outcome and the reasoning.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("staff/data-requests/{id}/close")]
    [RequireScope(Scopes.Customer.Write)]
    [ProducesResponseType<DataRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DataRequestDto>> CloseDataRequest(
        string id,
        [FromBody] CloseDataRequestBody request,
        [FromServices] CloseDataRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var closed = await handler.HandleAsync(
            new CloseDataRequestCommand(
                id, request.Outcome, request.Resolution, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(closed.Version);

        return Ok(closed);
    }
}
