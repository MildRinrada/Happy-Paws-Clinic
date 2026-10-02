using FluentValidation;
using HappyPaws.Application.Access.Dtos;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Access.ReplaceRolePermissions;

/// <summary>
/// <c>PUT /staff/roles/{id}/permissions</c> — "แทนที่ทั้งชุด · ตรวจ scope ที่ไม่มีจริง".
/// </summary>
public sealed record ReplaceRolePermissionsCommand(
    string RoleId,
    IReadOnlyList<string> Scopes,
    uint? IfMatchVersion = null);

public sealed class ReplaceRolePermissionsValidator : AbstractValidator<ReplaceRolePermissionsCommand>
{
    public ReplaceRolePermissionsValidator()
    {
        // An empty set is legal here, unlike at creation: emptying a role is how a clinic retires
        // one without an endpoint to delete it.
        RuleFor(command => command.Scopes)
            .NotNull()
            .Must(scopes => scopes is null || ScopeCatalogue.UnknownIn(scopes).Count == 0)
            .WithMessage(command =>
                $"ไม่รู้จัก scope: {string.Join(", ", ScopeCatalogue.UnknownIn(command.Scopes ?? []))}");
    }
}

/// <summary>
/// Replaces a role's whole scope set.
/// <para>
/// A replacement, not a merge — the request is the role as it should be afterwards. Everyone
/// holding the role changes with it, on their next token: scopes travel in the access token, so
/// the edit reaches each member within its fifteen-minute lifetime without anyone being signed
/// out.
/// </para>
/// <para>
/// Two refusals protect the instance from its own administrators. The caller must hold everything
/// the role had <i>and</i> everything it is being given (<see cref="ScopeGrantPolicy"/>), and the
/// edit is rolled back if it would leave no active account with <c>*</c>
/// (<see cref="OwnerLockoutGuard"/>).
/// </para>
/// </summary>
public sealed class ReplaceRolePermissionsHandler(
    IRoleRepository roles,
    ScopeGrantPolicy grants,
    OwnerLockoutGuard lockout,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<RoleDto> HandleAsync(
        ReplaceRolePermissionsCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Role? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var role = await roles.FindTrackedByIdAsync(command.RoleId, token)
                ?? throw new NotFoundException("role", command.RoleId);

            if (role.IsPortalRole)
            {
                throw new RoleNotEditableException();
            }

            ConcurrencyGuard.Ensure(role, command.IfMatchVersion);

            var before = role.Scopes.ToArray();

            grants.EnsureCallerHolds(before.Concat(command.Scopes));

            var ownerCheck = await lockout.BeforeChangeAsync(token);

            role.ReplaceScopes(command.Scopes);

            audit.Record(new AuditEvent(
                AuditActions.RolePermissionsReplaced,
                AuditResources.Role,
                role.Id,
                Details: new Dictionary<string, object?>
                {
                    ["name"] = role.Name,
                    ["before"] = before,
                    ["after"] = role.Scopes.ToArray(),
                }));

            await ownerCheck.EnsureAnOwnerRemainsAsync(token);

            updated = role;
        }, cancellationToken);

        return RoleDto.From(updated!);
    }
}

/// <summary>
/// <c>409 role_not_editable</c> — the starter <c>customer</c> role describes the portal audience
/// and has no scope set to edit. Not in the API reference's error list.
/// </summary>
public sealed class RoleNotEditableException : AppException
{
    public RoleNotEditableException()
        : base("role_not_editable", "role นี้แก้ไขสิทธิ์ไม่ได้", HttpStatus.Conflict)
    {
        MessageEn = "This role's permissions cannot be edited.";
    }
}
