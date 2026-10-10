using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Customers;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Portal;

/// <summary><c>POST /portal/reviews</c>.</summary>
/// <param name="VisitId">From the pending list.</param>
/// <param name="Rating">1 to 5.</param>
/// <param name="Text">Optional, plain text, at most 1,000 characters.</param>
/// <param name="DisplayName"><c>first_name</c> or <c>anonymous</c>.</param>
public sealed record SubmitPortalReviewCommand(string? VisitId, int? Rating, string? Text, string? DisplayName);

/// <summary>
/// The signed-in customer reviews one of their own visits. Not in the API reference: see
/// <c>docs/contract-extensions.md § 36</c>.
/// <para>
/// A visit that is not theirs, or is not a visit, is <c>404</c>, the same answer either way so the
/// endpoint cannot be used to probe other customers' records. One already reviewed is
/// <c>409 review_already_submitted</c>; one older than 60 days is <c>422</c> on <c>visit_id</c>.
/// The review is pending until staff decide, and cannot be changed afterwards.
/// </para>
/// </summary>
public sealed class SubmitPortalReviewHandler(
    IReviewVisitReader visits,
    IReviewRepository reviews,
    ICustomerRepository customers,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<SubmittedReviewDto> HandleAsync(SubmitPortalReviewCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        if (string.IsNullOrWhiteSpace(command.VisitId))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]> { ["visit_id"] = ["ต้องระบุการมาที่จะรีวิว"] });
        }

        var input = ReviewRules.Check(command.Rating, command.Text, command.DisplayName);
        var now = clock.UtcNow;
        var today = ReviewRules.ClinicToday(now, clock.ClinicTimeZone);

        var visit = await visits.FindAsync(command.VisitId.Trim(), cancellationToken);

        if (visit is null || visit.CustomerId != customerId)
        {
            throw new NotFoundException("visit", command.VisitId);
        }

        if (visit.Reviewed)
        {
            throw ReviewRules.AlreadyReviewed();
        }

        if (visit.Day < ReviewRules.FirstReviewableDay(today))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["visit_id"] = [$"รีวิวได้ภายใน {ReviewRules.WindowDays} วันหลังการมา"],
            });
        }

        var customer = await customers.FindByIdAsync(customerId, cancellationToken);
        var review = CustomerReview.Submit(
            customerId,
            visit.BranchId,
            visit.Day,
            visit.VisitId,
            visit.PetId,
            visit.PetSpecies,
            input.Rating,
            input.Text,
            input.NameChoice,
            customer?.DisplayName,
            ReviewSource.Portal,
            invitationId: null,
            now);

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            reviews.Add(review);
            return Task.CompletedTask;
        }, cancellationToken);

        return SubmittedReviewDto.From(review);
    }
}
