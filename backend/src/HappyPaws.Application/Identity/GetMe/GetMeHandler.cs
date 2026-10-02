using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Identity.Capabilities;
using HappyPaws.Application.Identity.Dtos;

namespace HappyPaws.Application.Identity.GetMe;

/// <summary>
/// <c>GET /me</c> — profile, memberships, scopes and account status.
/// </summary>
public sealed class GetMeHandler(
    IIdentityRepository repository,
    EffectiveScopeResolver scopeResolver,
    IPetAccessPolicy petAccess,
    ICurrentUser currentUser)
{
    public async Task<MeDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("no subject on token");

        var account = await repository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("account", userId);

        var membership = await repository.FindStaffMembershipAsync(account.Id, cancellationToken);

        var memberships = new List<MembershipDto>();

        if (membership is { IsActive: true })
        {
            var roles = await repository.FindRolesAsync(membership.RoleIds, cancellationToken);
            var scopes = await scopeResolver.ResolveAsync(membership, customerId: null, cancellationToken);

            memberships.Add(new MembershipDto(
                Type: "staff",
                StaffId: membership.Id,
                StaffType: membership.StaffType,
                LicenseNo: membership.LicenseNo,
                CustomerId: null,
                Roles: [.. roles.Select(role => role.Name)],
                Branches: membership.BranchIds,
                Scopes: [.. scopes]));
        }

        // The contract shows both memberships side by side on one account.
        if (await repository.FindCustomerIdAsync(account.Id, cancellationToken) is { } customerId)
        {
            memberships.Add(new MembershipDto(
                Type: "customer",
                StaffId: null,
                StaffType: null,
                LicenseNo: null,
                CustomerId: customerId,
                Roles: [],
                Branches: [],
                Scopes: [Domain.Access.Scopes.Portal],
                Pets: [.. await petAccess.PetIdsForAsync(customerId, cancellationToken)]));
        }

        return new MeDto(
            account.Id,
            account.DisplayName,
            account.Status.ToString().ToLowerInvariant(),
            memberships,
            new TwoFactorDto(
                account.TwoFactorEnabled,
                account.TwoFactorMethod.ToString().ToLowerInvariant(),
                account.RecoveryCodeHashes.Count),
            account.Email,
            account.PhoneNumber,
            account.Language,
            new NotificationChannelsDto(account.NotifyByEmail, account.NotifyBySms, account.NotifyByLine),
            account.Version);
    }
}
