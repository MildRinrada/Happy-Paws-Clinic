using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Identity.Register;

/// <summary>
/// Gives a new customer login its customer record. Shared by sign-up (<c>POST /auth/register</c>)
/// and booking without signing up first (<c>POST /booking/guest/verify</c>,
/// <c>docs/contract-extensions.md § 27</c>), so the two can never disagree about who a number is.
/// <para>
/// <b>The record the counter made is reused, not duplicated.</b> A customer registered at the desk
/// last year should see last year's pets and bills, so the login is linked to the unlinked record
/// with the same number, and an invitation sent to that record is marked accepted. Only when there
/// is none is a new record made.
/// </para>
/// </summary>
public sealed class CustomerRecordLinker(
    ICustomerRepository customers,
    ICustomerInvitationRepository invitations,
    IWebhookOutbox webhooks,
    IClock clock)
{
    /// <summary>Must run inside the caller's transaction, after the account has been added.</summary>
    public async Task<(Customer Customer, bool LinkedExisting)> LinkAsync(
        UserAccount account,
        string phone,
        CancellationToken cancellationToken = default)
    {
        var existing = (await customers.UnlinkedWithPhoneDigitsAsync(phone, cancellationToken)).FirstOrDefault();

        if (existing is not null)
        {
            existing.LinkAccount(account.Id);

            if (await invitations.LatestForCustomerAsync(existing.Id, cancellationToken) is { } invitation)
            {
                invitation.Accept(clock.UtcNow);
            }

            return (existing, true);
        }

        var created = Customer.Create(account.DisplayName, phone, account.Email, account.Id);
        customers.Add(created);
        webhooks.Enqueue(WebhookEventTypes.CustomerCreated, WebhookPayloads.Customer(created, clock.UtcNow));

        return (created, false);
    }
}
