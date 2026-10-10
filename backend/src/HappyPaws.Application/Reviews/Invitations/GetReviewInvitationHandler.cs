using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.Application.Reviews.Invitations;

/// <summary>
/// <c>GET /reviews/invitations/{token}</c>: what an invitation link is for and whether it still
/// works. Public: the token is the credential. Not in the API reference: see
/// <c>docs/contract-extensions.md § 36</c>.
/// <para>
/// It names the visit by its clinic-local day, the service and the pet's name, and nothing else
/// about the customer: whoever holds the link sees no surname, phone or address. A token that is not
/// one of ours is <c>404</c>, whether it is malformed or simply unknown. A link whose visit no longer
/// counts (its bill was voided) reads as expired: there is nothing left to review.
/// </para>
/// </summary>
public sealed class GetReviewInvitationHandler(
    IReviewInvitationRepository invitations,
    IReviewVisitReader visits,
    IClock clock)
{
    public async Task<ReviewInvitationDto> HandleAsync(string? token, CancellationToken cancellationToken = default)
    {
        var invitation = await FindAsync(invitations, token, cancellationToken);
        var visit = await visits.FindAsync(invitation.VisitId, cancellationToken);

        var state = visit is null
            ? ReviewInvitationState.Expired
            : invitation.StateAt(clock.UtcNow, visit.Reviewed);

        return new ReviewInvitationDto(
            WireEnum.Of(state),
            invitation.ExpiresAt,
            new InvitationVisitDto(invitation.VisitOn, invitation.ServiceName, visit?.PetName));
    }

    /// <summary>The invitation a presented token opens, or <c>404</c>. The token itself is never logged.</summary>
    internal static async Task<ReviewInvitation> FindAsync(
        IReviewInvitationRepository invitations,
        string? token,
        CancellationToken cancellationToken)
    {
        if (!ReviewInvitation.LooksLikeToken(token))
        {
            throw new NotFoundException("review_invitation", "token");
        }

        return await invitations.FindByTokenHashAsync(ReviewInvitation.Hash(token!), cancellationToken)
            ?? throw new NotFoundException("review_invitation", "token");
    }
}
