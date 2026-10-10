using System.Text.Json.Serialization;
using HappyPaws.Application.Identity.Dtos;

namespace HappyPaws.Api.Contracts.Identity;

/// <summary>Request body for <c>POST /auth/staff/login</c>.</summary>
public sealed record StaffLoginRequest(string Email, string Password);

/// <summary>Request body for <c>POST /auth/2fa/verify</c>.</summary>
public sealed record VerifyTwoFactorRequest(string Challenge, string Code);

/// <summary>Request body for <c>POST /auth/logout</c>.</summary>
public sealed record LogoutRequest(bool AllDevices = false);

/// <summary>Request body for <c>POST /me/password</c>.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>
/// Request body for <c>PATCH /me</c> — "ชื่อ อีเมล เบอร์ ภาษา การแจ้งเตือน". Send only what changes;
/// <c>current_password</c> is required when <c>email</c> or <c>phone</c> does.
/// </summary>
public sealed record UpdateProfileRequest(
    string? DisplayName = null,
    string? Email = null,
    string? Phone = null,
    string? Language = null,
    NotificationChannelsRequest? Notifications = null,
    string? CurrentPassword = null);

/// <summary>Which channels to accept messages on. A channel left out keeps its setting.</summary>
public sealed record NotificationChannelsRequest(bool? Email = null, bool? Sms = null, bool? Line = null);

/// <summary>
/// Request body for <c>POST /me/2fa/enroll</c>. First call: <c>current_password</c>. Second call:
/// <c>code</c> from the authenticator app.
/// </summary>
public sealed record EnrolTwoFactorRequest(string? CurrentPassword = null, string? Code = null);

/// <summary>Request body for <c>PATCH /staff/users/{id}/status</c>.</summary>
public sealed record SetUserStatusRequest(string Status);

/// <summary>
/// What a completed sign-in returns.
/// <para>
/// The refresh token is <b>not</b> here — it goes into an httpOnly cookie. Returning it in the
/// body as well would hand script the very value the cookie flag exists to hide.
/// </para>
/// <para>
/// <c>capabilities</c> is what <c>POST /auth/login</c> is specified to return ("คืน capabilities
/// ของบัญชีในคลินิกนี้"): the memberships and scopes on the token just issued.
/// </para>
/// </summary>
public sealed record AuthTokensResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_at")] DateTimeOffset ExpiresAt,
    [property: JsonPropertyName("two_factor_satisfied")] bool TwoFactorSatisfied,
    [property: JsonPropertyName("capabilities")] SessionCapabilities? Capabilities = null)
{
    public static AuthTokensResponse From(AuthTokens tokens) =>
        new(tokens.AccessToken, "Bearer", tokens.AccessTokenExpiresAt, tokens.TwoFactorSatisfied, tokens.Capabilities);
}

/// <summary><c>POST /auth/otp/request</c>. <c>purpose</c> is <c>register</c> or <c>login</c>.</summary>
public sealed record RequestOtpRequest(string Phone, string Purpose);

/// <summary>
/// <c>POST /auth/register</c>: the phone, the code sent to it, and a name. Password and email are
/// optional. <c>referral_code</c> is an optional field this project added: a friend's code
/// (<c>docs/contract-extensions.md § 39</c>).
/// </summary>
public sealed record RegisterRequest(
    string Phone,
    string Code,
    string DisplayName,
    string? Password = null,
    string? Email = null,
    string? ReferralCode = null);

/// <summary>
/// <c>POST /auth/login</c>. Either <c>phone</c> with <c>code</c>, or <c>identifier</c> (an email
/// or a phone number) with <c>password</c>.
/// </summary>
public sealed record LoginRequest(
    string? Phone = null,
    string? Code = null,
    string? Identifier = null,
    string? Password = null);

/// <summary>
/// <c>POST /auth/password/reset</c>. First call: <c>identifier</c> to be sent a link. Second call:
/// the link's <c>token</c> and <c>new_password</c>.
/// </summary>
public sealed record ResetPasswordRequest(string? Identifier = null, string? Token = null, string? NewPassword = null);

/// <summary>
/// What <c>POST /auth/staff/login</c> returns when the account has 2FA enrolled: a challenge and
/// no bearer token, because the contract says the real token comes from <c>/auth/2fa/verify</c>.
/// </summary>
public sealed record TwoFactorChallengeResponse(
    [property: JsonPropertyName("two_factor_required")] bool TwoFactorRequired,
    [property: JsonPropertyName("challenge")] string Challenge,
    [property: JsonPropertyName("methods")] IReadOnlyList<string> Methods);
