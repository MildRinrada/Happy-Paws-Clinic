using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Customers;
using HappyPaws.Api.Http;
using HappyPaws.Application.Customers.LineAddress;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// A customer's LINE address, set at the counter from the recent followers of the clinic's LINE
/// account. Not in the API reference; see <c>docs/contract-extensions.md § 18</c>.
/// <para>
/// <b>Delivery information, not identity</b> (<c>docs/assumptions.md § A</c>): the counter picks an
/// address as it would type an email. Nothing here signs anyone in or links an account, and both
/// routes are <c>customer:write</c>, the scope that already edits a customer's contact details.
/// </para>
/// </summary>
[ApiController]
[Route("staff/customers/{id}/line")]
[RequireScope(Scopes.Customer.Write)]
public sealed class CustomerLineController : ControllerBase
{
    /// <summary>
    /// <c>PUT /staff/customers/{id}/line</c> — save the picked follower's address on the record.
    /// <c>422</c> for an address not on the follower list or one that blocked the account,
    /// <c>409 line_address_taken</c> (with <c>details.customer_id</c>) for one on another record.
    /// <c>If-Match</c> takes the customer's version.
    /// </summary>
    /// <param name="id">Customer id.</param>
    /// <param name="request">The LINE user id from the follower list.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut]
    [ProducesResponseType<CustomerLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerLineDto>> Link(
        string id,
        [FromBody] LinkLineAddressRequest request,
        [FromServices] LinkLineAddressHandler handler,
        CancellationToken cancellationToken)
    {
        var line = await handler.HandleAsync(
            new LinkLineAddressCommand(id, request.LineUserId, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(line.Version);

        return Ok(line);
    }

    /// <summary>
    /// <c>DELETE /staff/customers/{id}/line</c> — take the address off; messages go by SMS or email.
    /// <c>204</c> also when there was none. <c>If-Match</c> takes the customer's version.
    /// </summary>
    /// <param name="id">Customer id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unlink(
        string id,
        [FromServices] UnlinkLineAddressHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new UnlinkLineAddressCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        return NoContent();
    }
}
