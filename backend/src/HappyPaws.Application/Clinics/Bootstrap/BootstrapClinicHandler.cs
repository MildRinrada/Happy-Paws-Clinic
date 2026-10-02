using System.Text.RegularExpressions;
using HappyPaws.Application.Access;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Clinics.Bootstrap;

/// <param name="OwnerEmail">The first owner's sign-in address. Required unless an owner already exists.</param>
/// <param name="OwnerName">Their name as it will appear on records.</param>
/// <param name="OwnerPassword">Their password, or null to have one generated and printed once.</param>
/// <param name="ClinicName">Sets the clinic's public name.</param>
/// <param name="BranchCode">Code for the first branch — <c>sukhumvit39</c>. Used only if there is no branch yet.</param>
/// <param name="BranchName">Name for the first branch.</param>
/// <param name="BranchRooms">
/// Consulting rooms at the first branch — <c>room_1</c>, <c>room_2</c>. Asked for here because the
/// contract can open a branch and list branches but has no endpoint that edits one, so a branch
/// created without rooms would never be able to have a queue ticket assigned to one.
/// </param>
public sealed record BootstrapClinicCommand(
    string? OwnerEmail = null,
    string? OwnerName = null,
    string? OwnerPassword = null,
    string? ClinicName = null,
    string? BranchCode = null,
    string? BranchName = null,
    IReadOnlyList<string>? BranchRooms = null);

/// <summary>What one run changed. Everything it found already in place is absent.</summary>
/// <param name="RolesCreated">Starter roles that did not exist yet.</param>
/// <param name="BranchId">The first branch, if this run created it.</param>
/// <param name="OwnerStaffId">The owner's staff id, if this run created the owner.</param>
/// <param name="Owner">The owner's credentials, if this run created the owner. Shown once.</param>
public sealed record BootstrapResult(
    IReadOnlyList<string> RolesCreated,
    string? BranchId,
    string? OwnerStaffId,
    IssuedCredentials? Owner);

/// <summary>
/// Turns an empty database into a clinic someone can sign in to. An operator command, not an
/// endpoint.
/// <para>
/// <b>Why this cannot be an endpoint.</b> Everything that creates a member of staff requires a
/// member of staff: an invitation needs <c>staff:manage</c>, a role needs <c>role:manage</c>. On a
/// fresh instance nobody holds either, and an HTTP route that created the first owner without
/// authentication would be a route that creates an owner without authentication — open to whoever
/// reached the host first. The contract has no such route and this adds none. The first owner is
/// made by someone who already has the server.
/// </para>
/// <para>
/// <b>Safe to run again.</b> Each part is created only if absent: a starter role that exists is
/// left exactly as the clinic has edited it, a branch is created only when there are none, and an
/// owner is created only when nobody active holds <c>clinic_owner</c>. A second run on a working
/// clinic changes nothing, so it can sit in a deploy script.
/// </para>
/// <para>
/// <b>The owner gets a second factor here</b>, not later. They hold <c>*</c>, the two-factor gate
/// refuses every scoped endpoint to a session that holds clinical scopes without one, and the
/// endpoint that would enrol one is not built — so an owner created without TOTP could sign in and
/// do nothing at all.
/// </para>
/// </summary>
public sealed partial class BootstrapClinicHandler(
    IRoleRepository roles,
    IBranchRepository branches,
    IClinicSettingsStore settings,
    IStaffRepository staff,
    IIdentityRepository identity,
    OperatorCredentials credentials,
    IAuditTrail audit,
    IUnitOfWork unitOfWork)
{
    private const string DefaultBranchCode = "main";

    public async Task<BootstrapResult> HandleAsync(
        BootstrapClinicCommand command,
        CancellationToken cancellationToken = default)
    {
        BootstrapResult? result = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (ownerRole, rolesCreated) = await EnsureStarterRolesAsync(token);

            if (command.ClinicName is { Length: > 0 } clinicName)
            {
                (await settings.ProfileForUpdateAsync(token)).Update(name: clinicName);
            }

            var branchId = await EnsureBranchAsync(command, token);
            var (ownerStaffId, owner) = await EnsureOwnerAsync(command, ownerRole, token);

            result = new BootstrapResult(rolesCreated, branchId, ownerStaffId, owner);
        }, cancellationToken);

        return result!;
    }

    private async Task<(Role OwnerRole, IReadOnlyList<string> Created)> EnsureStarterRolesAsync(
        CancellationToken cancellationToken)
    {
        var existing = await roles.FindByNamesAsync(
            [.. DefaultRoles.StarterScopes.Select(starter => starter.Key)], cancellationToken);

        var byName = existing.ToDictionary(role => role.Name, StringComparer.Ordinal);
        var created = new List<string>();

        foreach (var (name, scopes) in DefaultRoles.StarterScopes)
        {
            if (byName.ContainsKey(name))
            {
                continue;
            }

            var role = Role.Create(name, scopes, isSystem: true);

            roles.Add(role);
            byName[name] = role;
            created.Add(name);
        }

        return (byName[DefaultRoles.ClinicOwner], created);
    }

    private async Task<string?> EnsureBranchAsync(BootstrapClinicCommand command, CancellationToken cancellationToken)
    {
        if ((await branches.ListActiveAsync(cancellationToken)).Count > 0)
        {
            return null;
        }

        var code = string.IsNullOrWhiteSpace(command.BranchCode) ? DefaultBranchCode : command.BranchCode.Trim();

        // The same shape POST /clinic/branches accepts, because the code becomes part of the id.
        if (!BranchCodePattern().IsMatch(code))
        {
            throw Invalid("branch_code", "รหัสสาขาต้องเป็น a–z, 0–9, _ หรือ - ยาว 2–32 ตัวอักษร");
        }

        var branch = Branch.Create(
            code,
            string.IsNullOrWhiteSpace(command.BranchName) ? "สาขาหลัก" : command.BranchName,
            id: $"{EntityId.Branch}_{code}",
            examRooms: command.BranchRooms);

        branches.Add(branch);

        return branch.Id;
    }

    private async Task<(string? StaffId, IssuedCredentials? Owner)> EnsureOwnerAsync(
        BootstrapClinicCommand command,
        Role ownerRole,
        CancellationToken cancellationToken)
    {
        if (await staff.AnyActiveWithRoleAsync(ownerRole.Id, cancellationToken))
        {
            return (null, null);
        }

        if (string.IsNullOrWhiteSpace(command.OwnerEmail) || string.IsNullOrWhiteSpace(command.OwnerName))
        {
            throw Invalid("owner_email", "ยังไม่มีเจ้าของคลินิก ต้องระบุอีเมลและชื่อของเจ้าของ");
        }

        if (command.OwnerPassword is { Length: > 0 and < OperatorCredentials.MinimumPasswordLength })
        {
            throw Invalid(
                "owner_password",
                $"รหัสผ่านต้องยาวอย่างน้อย {OperatorCredentials.MinimumPasswordLength} ตัวอักษร");
        }

        var email = command.OwnerEmail.Trim().ToLowerInvariant();

        if (await identity.FindByEmailAsync(email, cancellationToken) is not null)
        {
            // An existing account is never promoted by a command-line flag. If it should be an
            // owner, an owner grants that; if it is locked out, reset its credentials.
            throw Invalid("owner_email", "อีเมลนี้มีบัญชีอยู่แล้ว");
        }

        var (hash, generated) = credentials.NewPassword(command.OwnerPassword);
        var account = UserAccount.CreateStaff(command.OwnerName.Trim(), email, hash);
        var issued = credentials.EnrolSecondFactor(account, generated);

        // No branches: the owner is not branch-bound, which is how BranchGuard reads an empty list.
        var membership = StaffMembership.Create(account.Id, "owner", [ownerRole.Id], []);

        identity.AddAccount(account);
        staff.Add(membership);

        audit.Record(new AuditEvent(
            AuditActions.ClinicBootstrapped,
            AuditResources.StaffMember,
            membership.Id,
            Details: new Dictionary<string, object?>
            {
                ["user_id"] = account.Id,
                ["email"] = account.Email,
            }));

        return (membership.Id, issued);
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    [GeneratedRegex("^[a-z0-9][a-z0-9_-]{1,31}$")]
    private static partial Regex BranchCodePattern();
}
