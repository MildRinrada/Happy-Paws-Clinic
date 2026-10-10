using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Staff;

/// <summary><c>POST /staff/reviews/{id}/reply</c>: <c>{ "text": "..." }</c>.</summary>
/// <param name="ReviewId">From the path.</param>
/// <param name="Text">The clinic's answer: plain text, 1 to 1,000 characters.</param>
/// <param name="IfMatchVersion">The review's <c>version</c>, when sent.</param>
public sealed record ReplyToReviewCommand(string ReviewId, string? Text, uint? IfMatchVersion = null);

/// <summary>
/// Writes the clinic's one public answer under a review, or rewrites it: sending again replaces the
/// answer. It never touches the customer's words. A pending review may be answered before it is
/// published; a rejected one cannot (<c>409 state_conflict</c>). Audited as <c>review.replied</c>
/// with the staff id, without the text. Not in the API reference: see
/// <c>docs/contract-extensions.md § 36</c>.
/// </summary>
public sealed class ReplyToReviewHandler(ReviewModeration moderation)
{
    public Task<StaffReviewDto> HandleAsync(ReplyToReviewCommand command, CancellationToken cancellationToken = default)
    {
        if (ReviewText.ReplyProblem(command.Text) is { } problem)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]> { ["text"] = [problem] });
        }

        return moderation.ApplyAsync(
            command.ReviewId,
            command.IfMatchVersion,
            (review, staffId, now) =>
            {
                var replaced = review.Reply(command.Text!, staffId, now);

                return new AuditEvent(
                    AuditActions.ReviewReplied,
                    AuditResources.Review,
                    review.Id,
                    PetId: review.PetId,
                    BranchId: review.BranchId,
                    Details: new Dictionary<string, object?>
                    {
                        ["replaced"] = replaced,
                        ["length"] = review.ReplyText?.Length ?? 0,
                    });
            },
            cancellationToken);
    }
}
