namespace HappyPaws.Api.Contracts.Staff;

/// <summary>
/// <c>POST /staff/members/invitations</c> — "ส่งอีเมล/SMS เชิญ · ลิงก์อายุ 7 วัน". <c>roles</c> are
/// role names and <c>branches</c> are branch ids, as <c>GET /me</c> prints them.
/// </summary>
public sealed record InviteStaffMemberRequest(
    string Email,
    string DisplayName,
    string StaffType,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string>? Branches = null,
    string? LicenseNo = null);

/// <summary>
/// <c>POST /staff/members/invitations/{id}/accept</c> — "ผู้ถูกเชิญตั้งรหัสผ่านและ 2FA".
/// <para>
/// Called twice. With <c>token</c> alone it returns the TOTP secret to set up; with
/// <c>password</c> and a <c>code</c> from the authenticator app it creates the account.
/// </para>
/// </summary>
public sealed record AcceptInvitationRequest(string Token, string? Password = null, string? Code = null);

/// <summary>
/// <c>PATCH /staff/members/{id}</c> — "ตำแหน่ง สาขา เลขใบประกอบวิชาชีพ". An absent field is left
/// alone; <c>roles</c> and <c>branches</c> each replace their whole set. <c>department</c> is an
/// extension (§ 10).
/// </summary>
public sealed record UpdateStaffMemberRequest(
    string? StaffType = null,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<string>? Branches = null,
    string? LicenseNo = null,
    string? Department = null);

/// <summary>
/// <c>PUT /staff/members/{id}/public-profile</c>. The whole profile; a field left out is cleared.
/// Extension, § 10.
/// </summary>
public sealed record ReplacePublicProfileRequest(
    string? JobTitle = null,
    string? Specialty = null,
    string? Bio = null,
    string? PhotoPath = null,
    int? CareerStartYear = null,
    bool ShowOnWebsite = false);

/// <summary><c>POST /staff/roles</c> — "สร้าง role พร้อมชุด scope".</summary>
public sealed record CreateRoleRequest(string Name, IReadOnlyList<string> Scopes);

/// <summary>
/// <c>PUT /staff/roles/{id}/permissions</c> — "แทนที่ทั้งชุด · ตรวจ scope ที่ไม่มีจริง". The role's
/// scopes as they should be afterwards, not a list of changes.
/// </summary>
public sealed record ReplaceRolePermissionsRequest(IReadOnlyList<string> Scopes);
