namespace HappyPaws.Application.Identity.Dtos;

/// <summary>
/// The <c>GET /me</c> payload: profile, memberships, effective scopes and account status,
/// matching the shape shown in the API reference.
/// <para>
/// <c>Email</c> to <c>Version</c> are what <c>PATCH /me</c> changes ("ชื่อ อีเมล เบอร์ ภาษา
/// การแจ้งเตือน"); the reference's example does not show them, so they come last. <c>Version</c> is
/// what <c>If-Match</c> on <c>PATCH /me</c> compares.
/// </para>
/// </summary>
public sealed record MeDto(
    string UserId,
    string DisplayName,
    string Status,
    IReadOnlyList<MembershipDto> Memberships,
    TwoFactorDto TwoFactor,
    string? Email = null,
    string? Phone = null,
    string Language = "th",
    NotificationChannelsDto? Notifications = null,
    uint Version = 0);

/// <summary>Which channels the person accepts messages on.</summary>
public sealed record NotificationChannelsDto(bool Email, bool Sms, bool Line);

/// <summary>
/// One membership on the account. <c>Type</c> is <c>staff</c> or <c>customer</c> — the contract
/// allows one account to hold both, so this is a list rather than a discriminator on the account.
/// Staff fields are null on a customer membership and vice versa. <c>Pets</c> is the customer's
/// pets, as in the contract's example (<c>"pets": ["pet_luna"]</c>).
/// </summary>
public sealed record MembershipDto(
    string Type,
    string? StaffId,
    string? StaffType,
    string? LicenseNo,
    string? CustomerId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Branches,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string>? Pets = null);

/// <param name="Enabled">Whether a second factor is set up.</param>
/// <param name="Method"><c>totp</c>, <c>sms</c> or <c>none</c>.</param>
/// <param name="RecoveryCodesLeft">Unused recovery codes, so the person knows when to make new ones.</param>
public sealed record TwoFactorDto(bool Enabled, string Method, int RecoveryCodesLeft = 0);

/// <summary>One row of <c>GET /me/sessions</c>: a device the account is signed in on.</summary>
public sealed record SessionDto(
    string Id,
    string? UserAgent,
    string? IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUsedAt,
    DateTimeOffset ExpiresAt,
    bool Current);
