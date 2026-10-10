using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Reviews;

/// <summary>
/// One customer's review of one visit (<c>docs/contract-extensions.md § 36</c>). Not in the API
/// reference.
/// <para>
/// <b>A visit</b> is a clinic day at one branch for one customer, with a closed consultation or a
/// bill that was not voided, for a pet. It is named by <see cref="VisitId"/>, the record that stands
/// for the day (the first closed consultation, or else the first such bill), and kept as
/// <c>(customer, branch, day)</c>, which is unique: one review per visit.
/// </para>
/// <para>
/// <b>The honesty rules live here.</b> A review is <see cref="ReviewStatus.Pending"/> until staff
/// publish or reject it. Rejecting needs one of the listed reasons (<see cref="ReviewRejectReason"/>),
/// never free text. Nobody can change the customer's words: there is no method that sets
/// <see cref="Text"/> or <see cref="Rating"/> after <see cref="Submit"/>, for staff or for the
/// customer. The clinic may answer once, publicly, beside it (<see cref="ReplyText"/>), and may
/// rewrite that answer.
/// </para>
/// </summary>
public sealed class CustomerReview : Entity
{
    private CustomerReview()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    /// <summary>The visit's clinic-local day.</summary>
    public DateOnly VisitOn { get; private set; }

    /// <summary>The consultation or bill that names the visit.</summary>
    public string VisitId { get; private set; } = string.Empty;

    public string? PetId { get; private set; }

    /// <summary>The pet's species when the review was written, the one detail about the pet the public page may show.</summary>
    public string? PetSpecies { get; private set; }

    public int Rating { get; private set; }

    /// <summary>What the customer wrote, cleaned. Null for a score with no words.</summary>
    public string? Text { get; private set; }

    public ReviewNameChoice NameChoice { get; private set; }

    /// <summary>The first name shown publicly, worked out when it was written; null shows "ลูกค้า".</summary>
    public string? PublicName { get; private set; }

    public ReviewSource Source { get; private set; }

    /// <summary>The invitation it came through, for <see cref="ReviewSource.Invitation"/>.</summary>
    public string? InvitationId { get; private set; }

    public ReviewStatus Status { get; private set; }

    /// <summary>When it was last published. Kept after a later rejection, as history.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>When staff last published or rejected it.</summary>
    public DateTimeOffset? ModeratedAt { get; private set; }

    public string? ModeratedByStaffId { get; private set; }

    public ReviewRejectReason? RejectReason { get; private set; }

    /// <summary>The clinic's one public answer.</summary>
    public string? ReplyText { get; private set; }

    public DateTimeOffset? RepliedAt { get; private set; }

    public string? RepliedByStaffId { get; private set; }

    public bool IsPublished => Status == ReviewStatus.Published;

    /// <summary>
    /// A customer's review of their visit, pending. The caller has checked that the visit is theirs,
    /// recent enough and not reviewed yet; the unique key on the visit catches a second one that
    /// races in.
    /// </summary>
    public static CustomerReview Submit(
        string customerId,
        string branchId,
        DateOnly visitOn,
        string visitId,
        string? petId,
        string? petSpecies,
        int rating,
        string? text,
        ReviewNameChoice nameChoice,
        string? customerName,
        ReviewSource source,
        string? invitationId,
        DateTimeOffset now)
    {
        if (!ReviewText.IsRating(rating))
        {
            throw Invalid($"คะแนนต้องอยู่ระหว่าง {ReviewText.MinRating} ถึง {ReviewText.MaxRating}");
        }

        if (ReviewText.Problem(text) is { } problem)
        {
            throw Invalid(problem);
        }

        if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(branchId) || string.IsNullOrWhiteSpace(visitId))
        {
            throw Invalid("รีวิวต้องผูกกับการมาใช้บริการหนึ่งครั้ง");
        }

        return new CustomerReview
        {
            Id = EntityId.New(EntityId.Review),
            CustomerId = customerId,
            BranchId = branchId,
            VisitOn = visitOn,
            VisitId = visitId,
            PetId = petId,
            PetSpecies = string.IsNullOrWhiteSpace(petSpecies) ? null : petSpecies.Trim(),
            Rating = rating,
            Text = ReviewText.Clean(text),
            NameChoice = nameChoice,
            PublicName = ReviewNames.PublicNameFor(nameChoice, customerName),
            Source = source,
            InvitationId = invitationId,
            Status = ReviewStatus.Pending,
            CreatedAt = now,
        };
    }

    /// <summary>Puts a pending review on the public site. Only a pending one: a rejected review stays rejected.</summary>
    public void Publish(string staffId, DateTimeOffset now)
    {
        if (Status != ReviewStatus.Pending)
        {
            throw new StateConflictException("review", WireName(Status), "publish");
        }

        Status = ReviewStatus.Published;
        PublishedAt = now;
        ModeratedAt = now;
        ModeratedByStaffId = staffId;
        UpdatedAt = now;
    }

    /// <summary>
    /// Keeps a pending review off the site, or takes a published one down, for a listed reason.
    /// Taking one down when the reviewer asks is <see cref="ReviewRejectReason.CustomerRequest"/>.
    /// </summary>
    public void Reject(ReviewRejectReason reason, string staffId, DateTimeOffset now)
    {
        if (Status == ReviewStatus.Rejected)
        {
            throw new StateConflictException("review", WireName(Status), "reject");
        }

        if (!Enum.IsDefined(reason))
        {
            throw Invalid("ต้องเลือกเหตุผลจากรายการ");
        }

        Status = ReviewStatus.Rejected;
        RejectReason = reason;
        ModeratedAt = now;
        ModeratedByStaffId = staffId;
        UpdatedAt = now;
    }

    /// <summary>Writes, or rewrites, the clinic's one public answer. Not on a rejected review.</summary>
    /// <returns>True when it replaced an earlier answer.</returns>
    public bool Reply(string text, string staffId, DateTimeOffset now)
    {
        if (Status == ReviewStatus.Rejected)
        {
            throw new StateConflictException("review", WireName(Status), "reply");
        }

        if (ReviewText.ReplyProblem(text) is { } problem)
        {
            throw Invalid(problem);
        }

        var replaced = ReplyText is not null;

        ReplyText = ReviewText.Clean(text);
        RepliedAt = now;
        RepliedByStaffId = staffId;
        UpdatedAt = now;

        return replaced;
    }

    private static string WireName(ReviewStatus status) => status.ToString().ToLowerInvariant();

    private static DomainException Invalid(string message) => new("invalid_review", message);
}
