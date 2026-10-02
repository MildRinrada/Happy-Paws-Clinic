namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// Mints the 15-minute access tokens described by the contract.
/// <para>
/// Login use cases depend on this port; the JWT details live in Infrastructure. Refresh-token
/// rotation is a separate concern because it needs persisted, individually revocable sessions —
/// see <c>GET /me/sessions</c> and <c>DELETE /me/sessions/{id}</c>.
/// </para>
/// </summary>
public interface IAccessTokenIssuer
{
    IssuedToken Issue(AccessTokenRequest request);
}

/// <summary>Everything that goes into an access token.</summary>
/// <param name="UserId">Account id, e.g. <c>usr_8fA2</c>.</param>
/// <param name="SessionId">The revocable device session this token belongs to.</param>
/// <param name="StaffId">Staff membership id, when the account has one.</param>
/// <param name="CustomerId">Customer membership id, when the account has one.</param>
/// <param name="BranchIds">Branches this staff membership is assigned to.</param>
/// <param name="Scopes">Effective scopes, already expanded from the account's roles.</param>
/// <param name="TwoFactorSatisfied">
/// Whether a 2FA step was completed. Staff login must pass <c>false</c> until
/// <c>/auth/2fa/verify</c> succeeds — the contract is explicit that only that call returns the
/// real token.
/// </param>
public sealed record AccessTokenRequest(
    string UserId,
    string SessionId,
    string? StaffId,
    string? CustomerId,
    IReadOnlyCollection<string> BranchIds,
    IReadOnlyCollection<string> Scopes,
    bool TwoFactorSatisfied);

/// <param name="Token">The bearer value.</param>
/// <param name="ExpiresAt">Absolute expiry, so the client can refresh before a call fails.</param>
public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);
