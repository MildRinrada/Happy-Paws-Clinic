using FluentValidation;
using HappyPaws.Application.Access;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.UpdateStaffMember;

/// <summary>
/// <c>PATCH /staff/members/{id}</c> — "ตำแหน่ง สาขา เลขใบประกอบวิชาชีพ". An absent field is left
/// alone.
/// </summary>
/// <param name="StaffId">The member being changed.</param>
/// <param name="StaffType">Their position.</param>
/// <param name="Roles">Role names — replaces the whole set.</param>
/// <param name="Branches">Branch ids — replaces the whole set.</param>
/// <param name="LicenseNo">Licence number; an empty string clears it.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
/// <param name="Department">Their team, a code from <c>Departments</c> (extension, § 10).</param>
public sealed record UpdateStaffMemberCommand(
    string StaffId,
    string? StaffType = null,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<string>? Branches = null,
    string? LicenseNo = null,
    uint? IfMatchVersion = null,
    string? Department = null);

public sealed class UpdateStaffMemberValidator : AbstractValidator<UpdateStaffMemberCommand>
{
    public UpdateStaffMemberValidator()
    {
        RuleFor(command => command.StaffType)
            .NotEmpty().MaximumLength(64)
            .When(command => command.StaffType is not null);

        // Deactivating is how access is taken away. A member left in place with no roles would be
        // an account that signs in to nothing and that nobody remembers to close.
        RuleFor(command => command.Roles)
            .NotEmpty().WithMessage("ต้องมี role อย่างน้อยหนึ่งรายการ")
            .When(command => command.Roles is not null);

        RuleFor(command => command.LicenseNo).MaximumLength(64);

        RuleFor(command => command.Department)
            .Must(Departments.IsKnown)
            .WithMessage($"แผนกต้องเป็นหนึ่งใน {string.Join(", ", Departments.All.Select(known => known.Code))}")
            .When(command => command.Department is not null);
    }
}

/// <summary>
/// Changes a member's position, roles, branches or licence number.
/// <para>
/// <b>Roles are changed here</b>, though the endpoint's description names only position, branch
/// and licence. The contract binds roles to a member at invitation and gives no other endpoint
/// that could rebind them, and "ตำแหน่ง" in a clinic is as much what someone may do as what they
/// are called — so this is where promoting a nurse lives. See <c>docs/assumptions.md § 26</c>.
/// </para>
/// <para>
/// A change to roles or branches reaches the member on their next access token, since both travel
/// in it. That is at most fifteen minutes and needs nobody signed out.
/// </para>
/// </summary>
public sealed class UpdateStaffMemberHandler(
    StaffMemberAccess access,
    StaffAssignmentResolver assignments,
    ScopeGrantPolicy grants,
    OwnerLockoutGuard lockout,
    IStaffRepository staff,
    IRoleRepository roles,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    RequestValidator validator)
{
    public async Task<StaffMemberDto> HandleAsync(
        UpdateStaffMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (membership, currentRoles) = await access.ForChangeAsync(command.StaffId, token);

            ConcurrencyGuard.Ensure(membership, command.IfMatchVersion);

            var before = new Dictionary<string, object?>
            {
                ["staff_type"] = membership.StaffType,
                ["license_no"] = membership.LicenseNo,
                ["department"] = membership.Department,
                ["roles"] = currentRoles.Select(role => role.Name).ToArray(),
                ["branches"] = membership.BranchIds.ToArray(),
            };

            var after = new Dictionary<string, object?>(before);

            if (command.StaffType is not null || command.LicenseNo is not null)
            {
                membership.UpdateProfile(command.StaffType, command.LicenseNo);

                after["staff_type"] = membership.StaffType;
                after["license_no"] = membership.LicenseNo;
            }

            if (command.Department is not null)
            {
                membership.MoveToDepartment(command.Department);

                after["department"] = membership.Department;
            }

            // Observed before the roles move, settled after everything else has.
            var ownerCheck = command.Roles is null ? null : await lockout.BeforeChangeAsync(token);

            if (command.Roles is not null)
            {
                var assigned = await assignments.RolesAsync(command.Roles, token);

                grants.EnsureCallerHolds(assigned.SelectMany(role => role.Scopes));
                membership.ReplaceRoles(assigned.Select(role => role.Id));

                after["roles"] = assigned.Select(role => role.Name).ToArray();
            }

            if (command.Branches is not null)
            {
                membership.ReplaceBranches(await assignments.BranchesAsync(command.Branches, token));

                after["branches"] = membership.BranchIds.ToArray();
            }

            audit.Record(new AuditEvent(
                AuditActions.StaffUpdated,
                AuditResources.StaffMember,
                membership.Id,
                Details: new Dictionary<string, object?> { ["before"] = before, ["after"] = after }));

            if (ownerCheck is not null)
            {
                await ownerCheck.EnsureAnOwnerRemainsAsync(token);
            }
        }, cancellationToken);

        return await ReadBackAsync(command.StaffId, cancellationToken);
    }

    /// <summary>
    /// Re-read after commit rather than mapped from the tracked entity, so <c>version</c> is the
    /// one the database assigned and the role names are the ones now stored.
    /// </summary>
    private async Task<StaffMemberDto> ReadBackAsync(string staffId, CancellationToken cancellationToken)
    {
        var row = await staff.FindRowAsync(staffId, cancellationToken)
            ?? throw new NotFoundException("staff_member", staffId);

        var roleNames = (await roles.ListAsync(cancellationToken)).ToDictionary(role => role.Id, role => role.Name);

        return StaffMemberDto.From(row, roleNames, storage);
    }
}
