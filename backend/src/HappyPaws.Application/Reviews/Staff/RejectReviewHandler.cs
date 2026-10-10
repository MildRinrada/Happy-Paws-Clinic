using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Staff;

/// <summary><c>POST /staff/reviews/{id}/reject</c>: <c>{ "reason": "personal_data" }</c>.</summary>
/// <param name="ReviewId">From the path.</param>
/// <param name="Reason">
/// One of <c>personal_data</c>, <c>offensive</c>, <c>off_topic</c>, <c>spam</c>, <c>customer_request</c>. Required.
/// </param>
/// <param name="IfMatchVersion">The review's <c>version</c>, when sent.</param>
public sealed record RejectReviewCommand(string ReviewId, string? Reason, uint? IfMatchVersion = null);

/// <summary>
/// Keeps a pending review off the site, or takes a published one down, for a listed reason only.
/// No reason, or one not on the list, is <c>422</c> on <c>reason</c>: there is no free-text reason,
/// because the average stays honest only if staff cannot take a review down for disliking it. A
/// review the customer asks to have removed is rejected with <c>customer_request</c>. Already
/// rejected is <c>409 state_conflict</c>. Audited as <c>review.rejected</c> with the staff id and the
/// reason. Not in the API reference: see <c>docs/contract-extensions.md § 36</c>.
/// </summary>
public sealed class RejectReviewHandler(ReviewModeration moderation)
{
    public Task<StaffReviewDto> HandleAsync(RejectReviewCommand command, CancellationToken cancellationToken = default)
    {
        if (!WireEnum.TryParse<ReviewRejectReason>(command.Reason, out var reason))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["reason"] = [$"ต้องเลือกเหตุผลจากรายการ: {WireEnum.Names<ReviewRejectReason>()}"],
            });
        }

        return moderation.ApplyAsync(
            command.ReviewId,
            command.IfMatchVersion,
            (review, staffId, now) =>
            {
                var wasPublished = review.IsPublished;
                review.Reject(reason, staffId, now);

                return new AuditEvent(
                    AuditActions.ReviewRejected,
                    AuditResources.Review,
                    review.Id,
                    PetId: review.PetId,
                    BranchId: review.BranchId,
                    Details: new Dictionary<string, object?>
                    {
                        ["reason"] = WireEnum.Of(reason),
                        ["reason_label"] = ReviewRejectReasons.Label(reason),
                        ["was_published"] = wasPublished,
                        ["rating"] = review.Rating,
                    });
            },
            cancellationToken);
    }
}
