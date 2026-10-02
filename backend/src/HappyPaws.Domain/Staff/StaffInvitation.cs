using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Staff;

/// <summary>
/// An offer of employment in this clinic's system — <c>POST /staff/members/invitations</c>,
/// "ส่งอีเมล/SMS เชิญ · ลิงก์อายุ 7 วัน".
/// <para>
/// <b>Unlike a customer invitation, this one carries a token</b>, and the difference is the
/// contract's. A customer proves who they are with an OTP to a phone the clinic already holds; a
/// new member of staff has no credential here at all until they accept, so the link is the
/// credential. Only its hash is stored — a leaked backup must not hand anyone a working invitation
/// to become a vet.
/// </para>
/// <para>
/// The invitation also carries everything the membership will be: position, roles, branches. It is
/// decided by the person who invited, not by the person who accepts — accepting sets a password
/// and a second factor ("ผู้ถูกเชิญตั้งรหัสผ่านและ 2FA") and nothing else.
/// </para>
/// </summary>
public sealed class StaffInvitation : Entity
{
    /// <summary>The contract's "ลิงก์อายุ 7 วัน".</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private readonly List<string> _roleIds = [];
    private readonly List<string> _branchIds = [];

    private StaffInvitation()
    {
    }

    /// <summary>Where the invitation went, and the address the account will sign in with.</summary>
    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string StaffType { get; private set; } = string.Empty;

    public string? LicenseNo { get; private set; }

    public IReadOnlyList<string> RoleIds => _roleIds;

    public IReadOnlyList<string> BranchIds => _branchIds;

    /// <summary>SHA-256 of the link's token. The token itself is sent once and never stored.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public string InvitedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset SentAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    /// <summary>The account accepting created. Null until then.</summary>
    public string? AcceptedUserId { get; private set; }

    /// <summary>
    /// The TOTP secret shown to the invitee and not yet proven. It moves onto the account only
    /// when a code generated from it verifies — so an invitee who closes the tab before scanning
    /// the QR code has lost nothing and is locked out of nothing.
    /// </summary>
    public string? PendingTwoFactorSecret { get; private set; }

    public bool IsAccepted => AcceptedAt is not null;

    public bool IsLiveAt(DateTimeOffset now) => !IsAccepted && now < ExpiresAt;

    public static StaffInvitation Send(
        string email,
        string displayName,
        string staffType,
        IEnumerable<string> roleIds,
        IEnumerable<string> branchIds,
        string? licenseNo,
        string tokenHash,
        string invitedByStaffId,
        DateTimeOffset now)
    {
        var invitation = new StaffInvitation
        {
            Id = EntityId.New(EntityId.StaffInvitation),
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            StaffType = staffType.Trim(),
            LicenseNo = string.IsNullOrWhiteSpace(licenseNo) ? null : licenseNo.Trim(),
            TokenHash = tokenHash,
            InvitedByStaffId = invitedByStaffId,
            SentAt = now,
            ExpiresAt = now + Lifetime,
        };

        invitation._roleIds.AddRange(roleIds.Distinct(StringComparer.Ordinal));
        invitation._branchIds.AddRange(branchIds.Distinct(StringComparer.Ordinal));

        return invitation;
    }

    /// <summary>
    /// Re-inviting the same address refreshes this row rather than stacking a second one. The old
    /// link stops working the moment the new one is issued, and any half-finished second-factor
    /// setup is discarded with it.
    /// </summary>
    public void Reissue(
        string displayName,
        string staffType,
        IEnumerable<string> roleIds,
        IEnumerable<string> branchIds,
        string? licenseNo,
        string tokenHash,
        string invitedByStaffId,
        DateTimeOffset now)
    {
        EnsureNotAccepted();

        DisplayName = displayName.Trim();
        StaffType = staffType.Trim();
        LicenseNo = string.IsNullOrWhiteSpace(licenseNo) ? null : licenseNo.Trim();
        TokenHash = tokenHash;
        InvitedByStaffId = invitedByStaffId;
        SentAt = now;
        ExpiresAt = now + Lifetime;
        PendingTwoFactorSecret = null;

        _roleIds.Clear();
        _roleIds.AddRange(roleIds.Distinct(StringComparer.Ordinal));
        _branchIds.Clear();
        _branchIds.AddRange(branchIds.Distinct(StringComparer.Ordinal));
    }

    /// <summary>
    /// Keeps the secret the invitee was first shown. Asking twice returns the same one, so a page
    /// refresh does not invalidate the QR code already scanned into an authenticator app.
    /// </summary>
    public string BeginTwoFactorSetup(Func<string> newSecret)
    {
        EnsureNotAccepted();

        return PendingTwoFactorSecret ??= newSecret();
    }

    public void Accept(string userId, DateTimeOffset now)
    {
        EnsureNotAccepted();

        AcceptedAt = now;
        AcceptedUserId = userId;

        // Spent. The account carries the secret from here; the invitation must not keep a copy.
        PendingTwoFactorSecret = null;
    }

    private void EnsureNotAccepted()
    {
        if (IsAccepted)
        {
            throw new StateConflictException(nameof(StaffInvitation), "accepted", "accept");
        }
    }
}
