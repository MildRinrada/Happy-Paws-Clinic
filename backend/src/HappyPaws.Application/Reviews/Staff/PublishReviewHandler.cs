using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Reviews.Staff;

/// <summary><c>POST /staff/reviews/{id}/publish</c>.</summary>
/// <param name="ReviewId">From the path.</param>
/// <param name="IfMatchVersion">The review's <c>version</c>, when sent.</param>
public sealed record PublishReviewCommand(string ReviewId, uint? IfMatchVersion = null);

/// <summary>
/// Puts a pending review on the public site, in the customer's own words, and into the average.
/// Only a pending review: a published one is already up and a rejected one stays down
/// (<c>409 state_conflict</c>). Audited as <c>review.published</c> with the staff id. Not in the API
/// reference: see <c>docs/contract-extensions.md § 36</c>.
/// </summary>
public sealed class PublishReviewHandler(ReviewModeration moderation)
{
    public Task<StaffReviewDto> HandleAsync(PublishReviewCommand command, CancellationToken cancellationToken = default) =>
        moderation.ApplyAsync(
            command.ReviewId,
            command.IfMatchVersion,
            (review, staffId, now) =>
            {
                review.Publish(staffId, now);

                return new AuditEvent(
                    AuditActions.ReviewPublished,
                    AuditResources.Review,
                    review.Id,
                    PetId: review.PetId,
                    BranchId: review.BranchId,
                    Details: new Dictionary<string, object?>
                    {
                        ["rating"] = review.Rating,
                        ["source"] = WireEnum.Of(review.Source),
                        ["visit_on"] = review.VisitOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    });
            },
            cancellationToken);
}
