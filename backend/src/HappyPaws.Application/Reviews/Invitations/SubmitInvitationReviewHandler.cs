using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Customers;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Invitations;

/// <summary><c>POST /reviews/invitations/{token}</c>.</summary>
/// <param name="Token">From the path.</param>
/// <param name="Rating">1 to 5.</param>
/// <param name="Text">Optional, plain text, at most 1,000 characters.</param>
/// <param name="DisplayName"><c>first_name</c> or <c>anonymous</c>.</param>
public sealed record SubmitInvitationReviewCommand(string? Token, int? Rating, string? Text, string? DisplayName);

/// <summary>
/// A review written through an invitation link, by a customer who has no portal account. Public:
/// the token is the credential, good for one review. Not in the API reference: see
/// <c>docs/contract-extensions.md § 36</c>.
/// <para>
/// No consent is asked: writing a review is the customer's own act, and the review is about the
/// clinic, not a record of their pet's care. An unknown token is <c>404</c>; a used one, or a
/// visit already reviewed in the portal, <c>409 review_already_submitted</c>; one past its 30 days
/// <c>409 review_invitation_expired</c>. Limited per address (<c>RateLimits:ReviewSubmitsPerIpPerHour</c>).
/// </para>
/// </summary>
public sealed class SubmitInvitationReviewHandler(
    IReviewInvitationRepository invitations,
    IReviewVisitReader visits,
    IReviewRepository reviews,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<SubmittedReviewDto> HandleAsync(SubmitInvitationReviewCommand command, CancellationToken cancellationToken = default)
    {
        var input = ReviewRules.Check(command.Rating, command.Text, command.DisplayName);
        var now = clock.UtcNow;
        CustomerReview? review = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var invitation = await GetReviewInvitationHandler.FindAsync(invitations, command.Token, token);
            var visit = await visits.FindAsync(invitation.VisitId, token) ?? throw ReviewRules.InvitationExpired();

            switch (invitation.StateAt(now, visit.Reviewed))
            {
                case ReviewInvitationState.Used:
                    throw ReviewRules.AlreadyReviewed();

                case ReviewInvitationState.Expired:
                    throw ReviewRules.InvitationExpired();
            }

            var customer = await customers.FindByIdAsync(visit.CustomerId, token);

            review = CustomerReview.Submit(
                visit.CustomerId,
                visit.BranchId,
                visit.Day,
                visit.VisitId,
                visit.PetId,
                visit.PetSpecies,
                input.Rating,
                input.Text,
                input.NameChoice,
                customer?.DisplayName,
                ReviewSource.Invitation,
                invitation.Id,
                now);

            reviews.Add(review);
            invitation.MarkUsed(review.Id, now);
        }, cancellationToken);

        return SubmittedReviewDto.From(review!);
    }
}
