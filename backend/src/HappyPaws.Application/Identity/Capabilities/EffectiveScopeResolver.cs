using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Identity.Capabilities;

/// <summary>
/// Expands a staff membership into the scope set that goes on its access token.
/// <para>
/// One place does this, so the token a user is issued and the capabilities the UI is told about
/// can never disagree. A menu that shows an action the token cannot perform is a support ticket;
/// the reverse is a security surprise.
/// </para>
/// </summary>
public sealed class EffectiveScopeResolver(IIdentityRepository repository)
{
    public async Task<ScopeSet> ResolveAsync(
        StaffMembership? membership,
        string? customerId,
        CancellationToken cancellationToken = default)
    {
        var scopes = new List<string>();

        if (membership is { IsActive: true } && membership.RoleIds.Count > 0)
        {
            var roles = await repository.FindRolesAsync(membership.RoleIds, cancellationToken);
            scopes.AddRange(roles.SelectMany(role => role.Scopes));
        }

        // A customer membership grants the portal marker and nothing else. Portal endpoints are
        // then narrowed further per pet, which no scope can express.
        if (customerId is not null)
        {
            scopes.Add(Scopes.Portal);
        }

        return new ScopeSet(scopes);
    }
}
