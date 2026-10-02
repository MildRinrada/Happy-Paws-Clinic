using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Customers.InviteToPortal;

/// <summary>
/// <c>POST /staff/customers/{id}/invite</c> — "ชวนให้เปิดใช้ portal และผูก LINE".
/// </summary>
public sealed record InviteToPortalCommand(string CustomerId);

/// <summary>
/// Asks a customer to open a portal account.
/// <para>
/// Re-inviting refreshes the existing invitation rather than stacking rows, so "have we asked them,
/// and is that still live" stays a single answerable question rather than a pile to interpret.
/// </para>
/// <para>
/// <b>Two halves of this endpoint's description behave differently.</b> "ชวนให้เปิดใช้ portal" is
/// what runs. "ผูก LINE" does not, by a standing decision rather than an omission: LINE is an
/// outbound channel in this platform and never an identity, so there is no account to link. See
/// <c>docs/assumptions.md § A</c>.
/// </para>
/// <para>
/// And what the customer does next needs saying plainly: they sign up through
/// <c>POST /auth/otp/request</c> → <c>POST /auth/register</c>, which are <b>not built</b> because
/// they need an SMS adapter. So today this endpoint records the invitation and sends the message —
/// through a channel that also logs rather than delivers. The invitation is real; the path it opens
/// is not walkable yet, and that is the SMS gap, not this one.
/// </para>
/// </summary>
public sealed class InviteToPortalHandler(
    ICustomerRepository customers,
    ICustomerInvitationRepository invitations,
    INotificationDispatcher notifications,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<InviteToPortalHandler> logger)
{
    public async Task<InvitationDto> HandleAsync(
        InviteToPortalCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;

        CustomerInvitation? invitation = null;
        Customer? customer = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            customer = await customers.FindTrackedByIdAsync(command.CustomerId, token)
                ?? throw new NotFoundException("customer", command.CustomerId);

            if (customer.UserId is not null)
            {
                throw new StateConflictException(nameof(Customer), "linked", "invite");
            }

            if (customer.IsMerged)
            {
                throw new StateConflictException(nameof(Customer), "merged", "invite");
            }

            var existing = await invitations.LatestForCustomerAsync(customer.Id, token);

            if (existing is null)
            {
                invitation = CustomerInvitation.Send(customer.Id, customer.Phone, staffId, now);
                invitations.Add(invitation);
            }
            else
            {
                existing.Resend(customer.Phone, staffId, now);
                invitation = existing;
            }

            audit.Record(new AuditEvent(
                AuditActions.CustomerInvited,
                AuditResources.Customer,
                customer.Id,
                Details: new Dictionary<string, object?>
                {
                    ["invitation_id"] = invitation.Id,
                    ["sent_to"] = invitation.SentTo,
                    ["expires_at"] = invitation.ExpiresAt,
                }));
        }, cancellationToken);

        await NotifyAsync(customer!, cancellationToken);

        return InvitationDto.From(invitation!, now);
    }

    private async Task NotifyAsync(Customer customer, CancellationToken cancellationToken)
    {
        try
        {
            await notifications.SendAsync(
                new NotificationRequest(
                    customer.Id,
                    NotificationTemplates.PortalInvitation,
                    new Dictionary<string, string> { ["display_name"] = customer.DisplayName }),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The invitation is recorded either way, and the counter can re-send. Failing the
            // request would leave the row written and the caller believing nothing happened.
            logger.LogError(
                exception, "Portal invitation notification failed for customer {CustomerId}", customer.Id);
        }
    }
}
