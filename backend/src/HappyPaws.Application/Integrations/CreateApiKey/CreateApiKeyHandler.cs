using FluentValidation;
using HappyPaws.Application.Access;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.ApiKeys;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.CreateApiKey;

/// <summary>
/// <c>POST /staff/api-keys</c>:
/// <code>{ "name": "ระบบบัญชี", "scopes": ["appointment:read"], "branch_ids": ["brn_…"], "expires_at": "2027-01-01T00:00:00+07:00" }</code>
/// <c>branch_ids</c> and <c>expires_at</c> are optional: the caller's own branches, and 90 days.
/// </summary>
public sealed record CreateApiKeyCommand(
    string Name,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string>? BranchIds = null,
    DateTimeOffset? ExpiresAt = null);

public sealed class CreateApiKeyValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("ต้องระบุชื่อ key")
            .MaximumLength(ApiKey.NameMaxLength);

        RuleFor(command => command.Scopes)
            .NotEmpty().WithMessage("ต้องเลือก scope อย่างน้อยหนึ่งรายการ")
            .Must(scopes => scopes.All(scope => ApiKeyScopeRules.MayCarry(scope?.Trim() ?? string.Empty)))
            .WithMessage(command =>
                "API key ถือ scope เหล่านี้ไม่ได้: "
                + string.Join(", ", command.Scopes.Where(scope => !ApiKeyScopeRules.MayCarry(scope?.Trim() ?? string.Empty))));

        RuleFor(command => command.BranchIds)
            .Must(ids => ids!.Count <= 50 && ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("รายการสาขาไม่ถูกต้อง")
            .When(command => command.BranchIds is not null);
    }
}

/// <summary>
/// Issues an API key and shows it once.
/// <para>
/// <b>Scopes.</b> Each must be one an API key may carry at all (<see cref="ApiKeyScopeRules"/>:
/// no 2FA-level scope, no <c>integration:manage</c> or <c>staff:manage</c>, no wildcard), else
/// <c>422</c>; and one the caller holds, else <c>403 insufficient_scope</c>, the same rule as
/// granting a role (<see cref="ScopeGrantPolicy"/>).
/// </para>
/// <para>
/// <b>Branches.</b> A branch-bound caller may name only their own branches, and naming none means
/// all of their own. A caller bound to no branch may name any active branch, or none for every
/// branch.
/// </para>
/// <para>
/// <b>Expiry.</b> Always set: 90 days unless asked, never more than 365, so a forgotten key stops
/// by itself. A key known to be leaked is stopped sooner with <c>DELETE /staff/api-keys/{id}</c>
/// (<c>docs/contract-extensions.md § 22</c>).
/// </para>
/// </summary>
public sealed class CreateApiKeyHandler(
    IApiKeyRepository keys,
    IBranchRepository branches,
    ScopeGrantPolicy grants,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<CreatedApiKeyDto> HandleAsync(CreateApiKeyCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var userId = currentUser.UserId ?? throw new ForbiddenException("no subject on token");
        var now = clock.UtcNow;

        var scopes = command.Scopes.Select(scope => scope.Trim()).Distinct(StringComparer.Ordinal).ToList();
        grants.EnsureCallerHolds(scopes);

        var branchIds = await ResolveBranchesAsync(command.BranchIds, cancellationToken);

        var expiresAt = command.ExpiresAt ?? now + ApiKey.DefaultLifetime;
        if (expiresAt <= now || expiresAt > now + ApiKey.MaxLifetime)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["expires_at"] = ["วันหมดอายุต้องอยู่ในอนาคตและไม่เกิน 365 วันนับจากวันนี้"],
            });
        }

        var (key, plaintext) = ApiKey.Issue(command.Name, scopes, branchIds, expiresAt, now, userId, staffId);

        keys.Add(key);

        audit.Record(new AuditEvent(
            AuditActions.ApiKeyCreated,
            AuditResources.ApiKey,
            key.Id,
            Details: new Dictionary<string, object?>
            {
                ["name"] = key.Name,
                ["prefix"] = key.Prefix,
                ["scopes"] = key.Scopes.ToArray(),
                ["branch_ids"] = key.BranchIds.ToArray(),
                ["expires_at"] = key.ExpiresAt,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedApiKeyDto.From(key, plaintext);
    }

    private async Task<IReadOnlyList<string>> ResolveBranchesAsync(
        IReadOnlyList<string>? requested,
        CancellationToken cancellationToken)
    {
        var ids = (requested ?? []).Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToList();

        if (branchGuard.VisibleBranchIds is { } own)
        {
            if (ids.Count == 0)
            {
                return [.. own];
            }

            if (ids.FirstOrDefault(id => !own.Contains(id)) is { } outside)
            {
                throw new ForbiddenException($"staff is not assigned to branch {outside}");
            }

            return ids;
        }

        if (ids.Count == 0)
        {
            return ids;
        }

        var missing = await branches.MissingAsync(ids, cancellationToken);
        if (missing.Count > 0)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["branch_ids"] = [$"ไม่พบสาขา: {string.Join(", ", missing)}"],
            });
        }

        return ids;
    }
}
