using HappyPaws.Application.Common;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews;

/// <summary>A visit the signed-in customer can still review: their own data, so the pet is named.</summary>
/// <param name="VisitId">Send this back as <c>visit_id</c>.</param>
/// <param name="VisitOn">The clinic-local day.</param>
/// <param name="BranchId">Where.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="ServiceName">The service, when there is one.</param>
/// <param name="PetId">The pet.</param>
/// <param name="PetName">Its name.</param>
/// <param name="PetSpecies">Its species.</param>
/// <param name="ReviewableUntil">The last day it can be reviewed.</param>
public sealed record PendingReviewVisitDto(
    string VisitId,
    DateOnly VisitOn,
    string BranchId,
    string BranchName,
    string? ServiceName,
    string PetId,
    string PetName,
    string PetSpecies,
    DateOnly ReviewableUntil)
{
    public static PendingReviewVisitDto From(ReviewableVisit visit) => new(
        visit.VisitId,
        visit.Day,
        visit.BranchId,
        visit.BranchName,
        visit.ServiceName,
        visit.PetId,
        visit.PetName,
        visit.PetSpecies,
        visit.Day.AddDays(ReviewRules.WindowDays));
}

/// <summary><c>GET /portal/reviews/pending</c>.</summary>
/// <param name="FirstName">What <c>first_name</c> would show, so the form can say it; null shows "ลูกค้า".</param>
/// <param name="Data">The visits, newest first.</param>
public sealed record PendingReviewsDto(string? FirstName, IReadOnlyList<PendingReviewVisitDto> Data);

/// <summary>What the customer sees after sending a review: pending, in their own words.</summary>
/// <param name="Id">The review.</param>
/// <param name="Status">Always <c>pending</c> when it is new.</param>
/// <param name="Rating">1 to 5.</param>
/// <param name="Text">Their words, cleaned.</param>
/// <param name="DisplayName">What the public page will show if it is published.</param>
/// <param name="CreatedAt">When it was written.</param>
public sealed record SubmittedReviewDto(string Id, string Status, int Rating, string? Text, string DisplayName, DateTimeOffset CreatedAt)
{
    public static SubmittedReviewDto From(CustomerReview review) => new(
        review.Id,
        WireEnum.Of(review.Status),
        review.Rating,
        review.Text,
        ReviewNames.Display(review.PublicName),
        review.CreatedAt);
}

/// <summary>The visit an invitation is for: the day, the service and the pet's name, and nothing else.</summary>
/// <param name="VisitOn">The clinic-local day.</param>
/// <param name="ServiceName">The service, when there is one.</param>
/// <param name="PetName">The pet's name.</param>
public sealed record InvitationVisitDto(DateOnly VisitOn, string? ServiceName, string? PetName);

/// <summary><c>GET /reviews/invitations/{token}</c>.</summary>
/// <param name="Status"><c>usable</c>, <c>used</c> or <c>expired</c>.</param>
/// <param name="ExpiresAt">When the link stops working.</param>
/// <param name="Visit">What it is for.</param>
public sealed record ReviewInvitationDto(string Status, DateTimeOffset ExpiresAt, InvitationVisitDto Visit);

/// <summary>The clinic's public answer under a review.</summary>
/// <param name="Text">What it says.</param>
/// <param name="RepliedAt">When it was written or last rewritten.</param>
public sealed record ReviewReplyDto(string Text, DateTimeOffset RepliedAt);

/// <summary>
/// One published review on the public site. Never the surname, phone, email or pet's name: the
/// first name or "ลูกค้า", the pet's species, the score, the words and the clinic's answer.
/// </summary>
/// <param name="Id">The review.</param>
/// <param name="Rating">1 to 5.</param>
/// <param name="Text">The customer's words, or null for a score alone.</param>
/// <param name="DisplayName">The first name, or "ลูกค้า".</param>
/// <param name="PetSpecies">The pet's species, when known.</param>
/// <param name="CreatedAt">When it was written.</param>
/// <param name="Reply">The clinic's answer, when there is one.</param>
public sealed record PublicReviewDto(
    string Id,
    int Rating,
    string? Text,
    string DisplayName,
    string? PetSpecies,
    DateTimeOffset CreatedAt,
    ReviewReplyDto? Reply)
{
    public static PublicReviewDto From(CustomerReview review) => new(
        review.Id,
        review.Rating,
        review.Text,
        ReviewNames.Display(review.PublicName),
        review.PetSpecies,
        review.CreatedAt,
        review.ReplyText is { } reply ? new ReviewReplyDto(reply, review.RepliedAt!.Value) : null);
}

/// <summary>The figures over published reviews only.</summary>
/// <param name="Average">The mean to one decimal, or null when nothing is published.</param>
/// <param name="Count">How many are published.</param>
/// <param name="ByRating">How many gave each score, keyed <c>"1"</c> to <c>"5"</c>.</param>
public sealed record ReviewSummaryDto(decimal? Average, int Count, IReadOnlyDictionary<string, int> ByRating)
{
    public static ReviewSummaryDto From(ReviewSummary summary) => new(
        summary.Average,
        summary.Count,
        summary.ByRating.ToDictionary(
            entry => entry.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entry => entry.Value,
            StringComparer.Ordinal));
}

/// <summary><c>GET /reviews</c>: a page of published reviews and the summary.</summary>
/// <param name="Data">The reviews, newest written first.</param>
/// <param name="NextCursor">For the next page.</param>
/// <param name="Summary">Over every published review, not just this page.</param>
public sealed record PublicReviewPageDto(IReadOnlyList<PublicReviewDto> Data, string? NextCursor, ReviewSummaryDto Summary);

/// <summary>The visit a review is about, as staff see it.</summary>
/// <param name="VisitId">The consultation or bill that names the visit.</param>
/// <param name="VisitOn">The clinic-local day.</param>
/// <param name="BranchId">Where.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="PetId">The pet.</param>
/// <param name="PetName">Its name.</param>
public sealed record StaffReviewVisitDto(string VisitId, DateOnly VisitOn, string BranchId, string? BranchName, string? PetId, string? PetName);

/// <summary>Who wrote it, for staff only: never on the public page.</summary>
/// <param name="Id">The customer.</param>
/// <param name="Name">Their full name.</param>
public sealed record StaffReviewerDto(string Id, string? Name);

/// <summary>The last staff decision on a review.</summary>
/// <param name="ModeratedAt">When.</param>
/// <param name="ModeratedByStaffId">By whom.</param>
/// <param name="PublishedAt">When it was last published.</param>
/// <param name="RejectReason">The listed reason, for a rejected review.</param>
/// <param name="RejectReasonLabel">Its Thai label.</param>
public sealed record ReviewModerationDto(
    DateTimeOffset? ModeratedAt,
    string? ModeratedByStaffId,
    DateTimeOffset? PublishedAt,
    string? RejectReason,
    string? RejectReasonLabel);

/// <summary>The clinic's answer as staff see it, with who wrote it.</summary>
/// <param name="Text">What it says.</param>
/// <param name="RepliedAt">When.</param>
/// <param name="RepliedByStaffId">By whom.</param>
public sealed record StaffReviewReplyDto(string Text, DateTimeOffset RepliedAt, string? RepliedByStaffId);

/// <summary>One review in the moderation list, with everything staff need to decide.</summary>
/// <param name="Id">The review.</param>
/// <param name="Status"><c>pending</c>, <c>published</c> or <c>rejected</c>.</param>
/// <param name="Rating">1 to 5.</param>
/// <param name="Text">The customer's words, exactly as kept. Staff cannot change them.</param>
/// <param name="DisplayName">What the public page shows.</param>
/// <param name="NameChoice"><c>first_name</c> or <c>anonymous</c>.</param>
/// <param name="PetSpecies">The species shown publicly.</param>
/// <param name="Source"><c>portal</c> or <c>invitation</c>.</param>
/// <param name="CreatedAt">When it was written.</param>
/// <param name="Visit">What it is about.</param>
/// <param name="Customer">Who wrote it.</param>
/// <param name="Moderation">The last decision.</param>
/// <param name="Reply">The clinic's answer.</param>
/// <param name="Version">For <c>If-Match</c>.</param>
public sealed record StaffReviewDto(
    string Id,
    string Status,
    int Rating,
    string? Text,
    string DisplayName,
    string NameChoice,
    string? PetSpecies,
    string Source,
    DateTimeOffset CreatedAt,
    StaffReviewVisitDto Visit,
    StaffReviewerDto Customer,
    ReviewModerationDto Moderation,
    StaffReviewReplyDto? Reply,
    uint Version)
{
    public static StaffReviewDto From(StaffReviewRow row)
    {
        var review = row.Review;

        return new StaffReviewDto(
            review.Id,
            WireEnum.Of(review.Status),
            review.Rating,
            review.Text,
            ReviewNames.Display(review.PublicName),
            WireEnum.Of(review.NameChoice),
            review.PetSpecies,
            WireEnum.Of(review.Source),
            review.CreatedAt,
            new StaffReviewVisitDto(review.VisitId, review.VisitOn, review.BranchId, row.BranchName, review.PetId, row.PetName),
            new StaffReviewerDto(review.CustomerId, row.CustomerName),
            new ReviewModerationDto(
                review.ModeratedAt,
                review.ModeratedByStaffId,
                review.PublishedAt,
                review.RejectReason is { } reason ? WireEnum.Of(reason) : null,
                review.RejectReason is { } label ? ReviewRejectReasons.Label(label) : null),
            review.ReplyText is { } reply ? new StaffReviewReplyDto(reply, review.RepliedAt!.Value, review.RepliedByStaffId) : null,
            review.Version);
    }
}
