using HappyPaws.Application.Access;
using HappyPaws.Domain.Access;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Access;

/// <summary>EF Core implementation of the role-management port.</summary>
internal sealed class RoleRepository(HappyPawsDbContext db) : IRoleRepository
{
    public void Add(Role role) => db.Set<Role>().Add(role);

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<Role>().AsNoTracking()
            .OrderByDescending(role => role.IsSystem)
            .ThenBy(role => role.Name)
            .ToListAsync(cancellationToken);

    public Task<Role?> FindTrackedByIdAsync(string roleId, CancellationToken cancellationToken = default) =>
        db.Set<Role>().FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        db.Set<Role>().AnyAsync(role => role.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Role>> FindByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default)
    {
        if (names.Count == 0)
        {
            return [];
        }

        var wanted = names.ToArray();

        return await db.Set<Role>().AsNoTracking()
            .Where(role => wanted.Contains(role.Name))
            .ToListAsync(cancellationToken);
    }
}
