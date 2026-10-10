using System.Security.Cryptography;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Domain.Reviews;

/// <summary>What a customer holding an invitation can still do with it.</summary>
public enum ReviewInvitationState
{
    /// <summary>Not used, not expired, and the visit has no review yet.</summary>
    Usable,

    /// <summary>A review was written for the visit, through this link or in the portal.</summary>
    Used,

    /// <summary>Older than <see cref="ReviewInvitation.Lifetime"/>.</summary>
    Expired,
}

/// <summary>
/// The one invitation a visit gets to be reviewed (<c>docs/contract-extensions.md § 36</c>), made
/// by the <c>visit_completed</c> reminder at the moment its message is sent. Not in the API
/// reference.
/// <para>
/// <b>One per visit.</b> <c>(customer, branch, day)</c> is unique, so two reminder rules on the
/// same trigger, or two instances running one rule, cannot invite the same visit twice: the second
/// insert fails and its message is not sent.
/// </para>
/// <para>
/// <b>The link.</b> A customer with a portal account is sent the portal's review page and needs no
/// token, so <see cref="TokenHash"/> is null. Anyone else gets <c>/review/{token}</c>: <c>hprv_</c>
/// and 256 random bits, shown once in the message and never stored. The database keeps its
/// SHA-256, the same rule as API keys, calendar links and waiting-room screens. The token is good
/// for one review and for <see cref="Lifetime"/>; it opens nothing else.
/// </para>
/// </summary>
public sealed class ReviewInvitation : Entity
{
    public const string TokenPrefix = "hprv_";

    /// <summary>How long a link works after it is sent.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private ReviewInvitation()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    public DateOnly VisitOn { get; private set; }

    /// <summary>The consultation or bill that names the visit.</summary>
    public string VisitId { get; private set; } = string.Empty;

    public string? PetId { get; private set; }

    /// <summary>The service as the invitation page names it, when the visit had one.</summary>
    public string? ServiceName { get; private set; }

    /// <summary>SHA-256 of the token, lowercase hex. Null when the customer was sent the portal link.</summary>
    public string? TokenHash { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>The reminder that sent it, for reading a log after a complaint.</summary>
    public string? DispatchId { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public string? ReviewId { get; private set; }

    /// <summary>
    /// A new invitation, and its token when it has one. The token goes into the message and nowhere
    /// else; only its hash is kept.
    /// </summary>
    public static (ReviewInvitation Invitation, string? Token) Create(
        string customerId,
        string branchId,
        DateOnly visitOn,
        string visitId,
        string? petId,
        string? serviceName,
        bool withToken,
        string? dispatchId,
        DateTimeOffset now)
    {
        var token = withToken ? NewToken() : null;

        var invitation = new ReviewInvitation
        {
            Id = EntityId.New(EntityId.ReviewInvitation),
            CustomerId = customerId,
            BranchId = branchId,
            VisitOn = visitOn,
            VisitId = visitId,
            PetId = petId,
            ServiceName = string.IsNullOrWhiteSpace(serviceName) ? null : serviceName.Trim(),
            TokenHash = token is null ? null : Hash(token),
            ExpiresAt = now + Lifetime,
            DispatchId = dispatchId,
            CreatedAt = now,
        };

        return (invitation, token);
    }

    public static string NewToken() =>
        TokenPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => ApiKey.Hash(token);

    /// <summary>Whether a presented token is shaped like one, before anything is looked up.</summary>
    public static bool LooksLikeToken(string? presented) =>
        presented is not null
        && presented.StartsWith(TokenPrefix, StringComparison.Ordinal)
        && presented.Length == TokenPrefix.Length + 43
        && presented.Skip(TokenPrefix.Length).All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    /// <summary>What the link can do at <paramref name="now"/>, given whether the visit already has a review.</summary>
    public ReviewInvitationState StateAt(DateTimeOffset now, bool visitReviewed)
    {
        if (UsedAt is not null || visitReviewed)
        {
            return ReviewInvitationState.Used;
        }

        return now >= ExpiresAt ? ReviewInvitationState.Expired : ReviewInvitationState.Usable;
    }

    /// <summary>Spends the invitation on the review written through it.</summary>
    public void MarkUsed(string reviewId, DateTimeOffset now)
    {
        if (UsedAt is not null)
        {
            throw new StateConflictException("review_invitation", "used", "use");
        }

        UsedAt = now;
        ReviewId = reviewId;
        UpdatedAt = now;
    }
}
