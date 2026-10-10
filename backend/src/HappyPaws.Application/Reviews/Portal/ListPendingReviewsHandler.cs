using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Customers;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Portal;

/// <summary>
/// <c>GET /portal/reviews/pending</c>: the signed-in customer's visits of the last 60 days that have
/// no review yet. Not in the API reference: see <c>docs/contract-extensions.md § 36</c>.
/// <para>
/// The customer comes from the token only. A visit of a pet since recorded deceased is left out:
/// asking for a score on that visit is not something the clinic should do unprompted.
/// </para>
/// </summary>
public sealed class ListPendingReviewsHandler(
    IReviewVisitReader visits,
    ICustomerRepository customers,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<PendingReviewsDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();
        var today = ReviewRules.ClinicToday(clock.UtcNow, clock.ClinicTimeZone);

        var found = await visits.ForCustomerAsync(customerId, ReviewRules.FirstReviewableDay(today), today, cancellationToken);
        var customer = await customers.FindByIdAsync(customerId, cancellationToken);

        return new PendingReviewsDto(
            ReviewNames.FirstNameOf(customer?.DisplayName),
            [.. found.Where(visit => !visit.Reviewed && visit.PetAlive).Select(PendingReviewVisitDto.From)]);
    }
}
