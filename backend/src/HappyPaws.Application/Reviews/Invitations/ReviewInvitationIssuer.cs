using HappyPaws.Application.Automation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Invitations;

/// <summary>
/// Makes the invitation a <c>visit_completed</c> reminder carries, at the moment it is sent
/// (<c>docs/contract-extensions.md § 36</c>). Called by the scheduler's real run only, never by a
/// simulation, so a preview creates nothing.
/// <list type="number">
/// <item>Consent first, through the same <see cref="ConsentGate"/> the dispatcher uses. A customer
/// who has not agreed to marketing gets no invitation and no token; the message is still handed to
/// the dispatcher, which records it skipped with <c>no_consent</c>, so the delivery log answers why.</item>
/// <item>A customer with a portal account is sent the portal's review page and needs no token.
/// Anyone else gets a one-time link; only its hash is kept.</item>
/// <item>The invitation row is the "one per visit" guarantee: if the visit already has one, from
/// another rule or another instance, nothing is sent.</item>
/// </list>
/// </summary>
public sealed class ReviewInvitationIssuer(
    ConsentGate consent,
    ICustomerRepository customers,
    IReviewInvitationRepository invitations,
    IReviewLinks links,
    IClock clock)
{
    /// <summary>The value the message's link is filled from.</summary>
    public const string LinkKey = "review_url";

    /// <summary>
    /// The request to send for this reminder: with its link when an invitation was made, unchanged
    /// when consent will stop it anyway, and null when the visit was already invited and nothing
    /// should be sent.
    /// </summary>
    public async Task<NotificationRequest?> PrepareAsync(
        AutomationOccurrence occurrence,
        string dispatchId,
        CancellationToken cancellationToken = default)
    {
        var request = occurrence.Request;
        var subject = occurrence.Subject;

        if (!await consent.MayReceiveAsync(request.CustomerId, request.TemplateKey, cancellationToken))
        {
            return request;
        }

        var customer = await customers.FindByIdAsync(subject.CustomerId, cancellationToken);
        var hasAccount = customer?.UserId is not null;

        var (invitation, token) = ReviewInvitation.Create(
            subject.CustomerId,
            subject.BranchId ?? string.Empty,
            subject.Day,
            subject.SubjectId,
            subject.PetId,
            subject.Label,
            withToken: !hasAccount,
            dispatchId,
            clock.UtcNow);

        if (!await invitations.TryAddAsync(invitation, cancellationToken))
        {
            return null;
        }

        var data = new Dictionary<string, string>(request.Data, StringComparer.Ordinal)
        {
            [LinkKey] = token is null ? links.Portal : links.ForToken(token),
        };

        return request with { Data = data };
    }
}
