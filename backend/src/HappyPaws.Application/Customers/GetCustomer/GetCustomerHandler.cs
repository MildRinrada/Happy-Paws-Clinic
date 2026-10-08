using HappyPaws.Application.Billing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Application.Customers.LineAddress;
using HappyPaws.Application.Integrations;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Patients.Dtos;

namespace HappyPaws.Application.Customers.GetCustomer;

/// <summary>
/// <c>GET /staff/customers/{id}</c> — "ข้อมูลติดต่อ สัตว์ทั้งหมด ยอดค้างชำระ".
/// </summary>
public sealed record GetCustomerQuery(string CustomerId);

/// <summary>
/// One customer, as the counter needs to see them.
/// <para>
/// Three reads in one response because the contract asks for three things in one row, and because
/// they are three parts of one glance: who this is, which animals are theirs, and whether to ask
/// them for money before they leave.
/// </para>
/// <para>
/// <b>The balance comes from Billing's own repository</b>, not from a sum written here. "What is
/// outstanding" is a billing rule — which statuses still count as owed, and whether a refunded bill
/// does — and a second implementation of it in the customer module is a second answer that can
/// drift from the one the invoice list shows.
/// </para>
/// <para>
/// Not audited, deliberately. A receptionist opens dozens of these an hour, and logging them would
/// drown <c>record.viewed</c> — the row the access log exists for. The line is drawn at health data:
/// the patient's timeline below is audited, this contact card is not. See
/// <c>Domain/Auditing/AuditActions.cs</c>.
/// </para>
/// </summary>
public sealed class GetCustomerHandler(
    ICustomerRepository customers,
    ICustomerInvitationRepository invitations,
    IPetRepository pets,
    IInvoiceRepository invoices,
    ILineFollowerRepository followers,
    IClock clock)
{
    public async Task<CustomerDetailDto> HandleAsync(
        GetCustomerQuery query,
        CancellationToken cancellationToken = default)
    {
        // A merged record still resolves. An id printed on last year's receipt should explain where
        // it went, not answer 404 as though the person never existed — the DTO carries
        // `merged_into_customer_id` for exactly that.
        var customer = await customers.FindByIdAsync(query.CustomerId, cancellationToken)
            ?? throw new NotFoundException("customer", query.CustomerId);

        var owned = await pets.ForCustomerAsync(customer.Id, cancellationToken);
        var outstanding = await invoices.OutstandingForCustomerAsync(customer.Id, cancellationToken);
        var invitation = await invitations.LatestForCustomerAsync(customer.Id, cancellationToken);
        var follower = customer.LineUserId is { } lineUserId
            ? await followers.FindAsync(lineUserId, cancellationToken)
            : null;

        return new CustomerDetailDto(
            CustomerDto.From(customer),
            [.. owned.Select(PetSummaryDto.From)],
            outstanding.Satang,
            invitation is null ? null : InvitationDto.From(invitation, clock.UtcNow),
            CustomerLineDto.From(customer, follower));
    }
}
