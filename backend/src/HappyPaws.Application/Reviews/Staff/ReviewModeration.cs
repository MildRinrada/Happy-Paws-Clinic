using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Staff;

/// <summary>
/// The shape every staff action on a review shares: load it, check <c>If-Match</c> when sent, act,
/// write the audit row in the same transaction, and answer with the review as the list shows it.
/// Every action names the member of staff, in the review and in the audit log.
/// </summary>
public sealed class ReviewModeration(
    IReviewRepository reviews,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<StaffReviewDto> ApplyAsync(
        string reviewId,
        uint? ifMatchVersion,
        Func<CustomerReview, string, DateTimeOffset, AuditEvent> act,
        CancellationToken cancellationToken)
    {
        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var review = await reviews.FindAsync(reviewId, token)
                ?? throw new NotFoundException("review", reviewId);

            ConcurrencyGuard.Ensure(review, ifMatchVersion);

            audit.Record(act(review, staffId, now));
        }, cancellationToken);

        var row = await reviews.FindRowAsync(reviewId, cancellationToken)
            ?? throw new NotFoundException("review", reviewId);

        return StaffReviewDto.From(row);
    }
}
