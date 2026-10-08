using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Customers.LineAddress;

/// <summary>
/// <c>DELETE /staff/customers/{id}/line</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 18</c>.
/// </summary>
/// <param name="CustomerId">The customer.</param>
/// <param name="IfMatchVersion">The customer's version, from <c>If-Match</c>.</param>
public sealed record UnlinkLineAddressCommand(string CustomerId, uint? IfMatchVersion = null);

/// <summary>
/// Takes the LINE address off a customer's record: messages go by SMS or email from now on, and the
/// address may be picked for another record. A record with no address is left as it is.
/// <para>
/// Allowed on a merged record too, so an address a tombstone still holds can be freed.
/// </para>
/// </summary>
public sealed class UnlinkLineAddressHandler(
    ICustomerRepository customers,
    IAuditTrail audit,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(UnlinkLineAddressCommand command, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var customer = await customers.FindTrackedByIdAsync(command.CustomerId, token)
                ?? throw new NotFoundException("customer", command.CustomerId);

            ConcurrencyGuard.Ensure(customer, command.IfMatchVersion);

            if (customer.LineUserId is not { } previous)
            {
                return;
            }

            customer.SetLineAddress(null);

            audit.Record(new AuditEvent(
                AuditActions.CustomerLineUnlinked,
                AuditResources.Customer,
                customer.Id,
                Details: new Dictionary<string, object?> { ["line_user_id"] = previous }));
        }, cancellationToken);
    }
}
