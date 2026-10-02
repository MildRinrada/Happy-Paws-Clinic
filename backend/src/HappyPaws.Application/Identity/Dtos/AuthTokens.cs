namespace HappyPaws.Application.Identity.Dtos;

/// <summary>
/// The result of a completed sign-in or refresh.
/// <para>
/// <see cref="RefreshToken"/> is returned to the API layer, which puts it in an httpOnly cookie
/// for web clients. It is never rendered into a JSON body for a browser — script-readable refresh
/// tokens defeat the point of rotating them.
/// </para>
/// </summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    string SessionId,
    bool TwoFactorSatisfied,
    SessionCapabilities? Capabilities = null);

/// <summary>
/// "คืน capabilities ของบัญชีในคลินิกนี้" — what the token just issued can do: the memberships it
/// carries and the scopes they expand to. The same facts the token holds, so a client can lay out
/// its menus without decoding a JWT.
/// </summary>
public sealed record SessionCapabilities(
    string? StaffId,
    string? CustomerId,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> Branches);

/// <summary>
/// The outcome of <c>POST /auth/staff/login</c>.
/// <para>
/// Either the sign-in completed, or a second factor is required and only a challenge is returned.
/// The contract is explicit that the real access token comes from <c>/auth/2fa/verify</c>, so the
/// challenge branch deliberately carries no bearer token at all.
/// </para>
/// </summary>
public sealed record StaffLoginResult(AuthTokens? Tokens, string? TwoFactorChallenge)
{
    public bool RequiresTwoFactor => TwoFactorChallenge is not null;

    public static StaffLoginResult Completed(AuthTokens tokens) => new(tokens, null);

    public static StaffLoginResult ChallengeRequired(string challenge) => new(null, challenge);
}
