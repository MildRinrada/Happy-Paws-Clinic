using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews;

/// <summary>
/// One visit as the review pages see it (<c>docs/contract-extensions.md § 36</c>).
/// <para>
/// A visit is a clinic day at one branch for one customer with a closed consultation or a bill
/// that was not voided, for a pet: the notion of a visit the due-back list and the customer groups
/// already use (a consultation or a bill that was not voided), narrowed to what can be reviewed.
/// An open consultation is a visit still going on, and a bill with no pet is a sale at the
/// counter. <see cref="VisitId"/> is the record that stands for the day: the first closed
/// consultation, or else the first such bill.
/// </para>
/// </summary>
/// <param name="VisitId">The consultation or bill that names the visit.</param>
/// <param name="CustomerId">Whose visit it is: the record a merged-away customer was merged into.</param>
/// <param name="BranchId">Where.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="Day">The clinic-local day.</param>
/// <param name="PetId">The pet seen.</param>
/// <param name="PetName">Its name.</param>
/// <param name="PetSpecies">Its species.</param>
/// <param name="PetAlive">False when the pet has since been recorded deceased.</param>
/// <param name="ServiceName">The booked service, or the first service on the bill, when there is one.</param>
/// <param name="Reviewed">True when the visit already has a review.</param>
public sealed record ReviewableVisit(
    string VisitId,
    string CustomerId,
    string BranchId,
    string BranchName,
    DateOnly Day,
    string PetId,
    string PetName,
    string PetSpecies,
    bool PetAlive,
    string? ServiceName,
    bool Reviewed);

/// <summary>Reads visits for the review pages, in SQL over consultations and bills. Read-only.</summary>
public interface IReviewVisitReader
{
    /// <summary>This customer's visits from <paramref name="firstDay"/> to <paramref name="lastDay"/>, newest first.</summary>
    Task<IReadOnlyList<ReviewableVisit>> ForCustomerAsync(
        string customerId,
        DateOnly firstDay,
        DateOnly lastDay,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The visit a consultation or bill belongs to, whoever's it is, or null when the id is neither
    /// or the record does not count as a visit.
    /// </summary>
    Task<ReviewableVisit?> FindAsync(string visitId, CancellationToken cancellationToken = default);
}

/// <summary>A review with what the moderation list shows beside it.</summary>
/// <param name="Review">The review.</param>
/// <param name="CustomerName">The reviewer's full name, for staff only.</param>
/// <param name="PetName">The pet's name, for staff only.</param>
/// <param name="BranchName">The branch of the visit.</param>
public sealed record StaffReviewRow(CustomerReview Review, string? CustomerName, string? PetName, string? BranchName);

/// <summary>Persistence port for customer reviews.</summary>
public interface IReviewRepository
{
    void Add(CustomerReview review);

    /// <summary>One review, tracked for change. Null when absent.</summary>
    Task<CustomerReview?> FindAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>One review with its names, for the answer of a staff action.</summary>
    Task<StaffReviewRow?> FindRowAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Every review of a status, or of every status, newest written first.</summary>
    Task<CursorPage<StaffReviewRow>> ListForStaffAsync(
        ReviewStatus? status,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>Published reviews, newest written first.</summary>
    Task<CursorPage<CustomerReview>> ListPublishedAsync(CursorPageRequest page, CancellationToken cancellationToken = default);

    /// <summary>How many published reviews gave each score.</summary>
    Task<IReadOnlyDictionary<int, int>> PublishedCountsByRatingAsync(CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for review invitations.</summary>
public interface IReviewInvitationRepository
{
    /// <summary>The invitation whose token hashes to this, tracked for change. Null when none.</summary>
    Task<ReviewInvitation?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new invitation at once. False when the visit already has one: the unique key on the
    /// visit decides, so it holds across instances and across rules.
    /// </summary>
    Task<bool> TryAddAsync(ReviewInvitation invitation, CancellationToken cancellationToken = default);
}

/// <summary>The addresses a review message links to, from configuration.</summary>
public interface IReviewLinks
{
    /// <summary>The portal's review page, for a customer with an account.</summary>
    string Portal { get; }

    /// <summary>The public invitation page for one token.</summary>
    string ForToken(string token);
}
