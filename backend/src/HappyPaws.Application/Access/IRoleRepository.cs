using HappyPaws.Domain.Access;

namespace HappyPaws.Application.Access;

/// <summary>
/// Persistence port for roles.
/// <para>
/// Sign-in reads roles through <c>IIdentityRepository</c>, which is shaped around "account,
/// membership, roles in one step". This is the management view: listing them, finding them by the
/// name a request uses, and loading one to change it.
/// </para>
/// </summary>
public interface IRoleRepository
{
    void Add(Role role);

    /// <summary>Every role, starter roles first, then the clinic's own by name. Untracked.</summary>
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Tracked, for the handlers that change a role.</summary>
    Task<Role?> FindTrackedByIdAsync(string roleId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Roles by the name a caller wrote — <c>"roles": ["veterinarian"]</c>, as <c>GET /me</c>
    /// prints them. Names that match nothing are simply absent from the result.
    /// </summary>
    Task<IReadOnlyList<Role>> FindByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default);
}
