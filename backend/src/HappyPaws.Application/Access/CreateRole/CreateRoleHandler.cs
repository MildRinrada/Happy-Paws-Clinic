using FluentValidation;
using HappyPaws.Application.Access.Dtos;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Access.CreateRole;

/// <summary><c>POST /staff/roles</c> — "สร้าง role พร้อมชุด scope".</summary>
public sealed record CreateRoleCommand(string Name, IReadOnlyList<string> Scopes);

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .Length(2, 64)
            .Must(name => name is null || name == name.Trim())
            .WithMessage("ชื่อ role ต้องไม่ขึ้นต้นหรือลงท้ายด้วยช่องว่าง");

        RuleFor(command => command.Scopes)
            .NotEmpty().WithMessage("ต้องระบุ scope อย่างน้อยหนึ่งรายการ")
            .Must(scopes => scopes is null || ScopeCatalogue.UnknownIn(scopes).Count == 0)
            .WithMessage(command =>
                $"ไม่รู้จัก scope: {string.Join(", ", ScopeCatalogue.UnknownIn(command.Scopes ?? []))}");
    }
}

/// <summary>
/// Creates a clinic-defined role.
/// <para>
/// This is why authorization checks scopes and never role names: the moment a clinic writes
/// "senior_nurse" here, every <c>[RequireScope]</c> in the system already knows what to do with
/// it, and nothing that compared a role name would.
/// </para>
/// </summary>
public sealed class CreateRoleHandler(
    IRoleRepository roles,
    ScopeGrantPolicy grants,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<RoleDto> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        grants.EnsureCallerHolds(command.Scopes);

        Role? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await roles.NameExistsAsync(command.Name, token))
            {
                throw new DuplicateRoleException(command.Name);
            }

            created = Role.Create(command.Name, command.Scopes);
            roles.Add(created);

            audit.Record(new AuditEvent(
                AuditActions.RoleCreated,
                AuditResources.Role,
                created.Id,
                Details: new Dictionary<string, object?>
                {
                    ["name"] = created.Name,
                    ["scopes"] = created.Scopes.ToArray(),
                }));
        }, cancellationToken);

        return RoleDto.From(created!);
    }
}

/// <summary><c>409 duplicate_role</c> — the name is taken. Not in the API reference's error list.</summary>
public sealed class DuplicateRoleException : AppException
{
    public DuplicateRoleException(string name)
        : base("duplicate_role", "มี role ชื่อนี้อยู่แล้ว", HttpStatus.Conflict)
    {
        MessageEn = $"A role named '{name}' already exists.";
        Field = "name";
    }
}
