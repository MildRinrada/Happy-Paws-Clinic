using HappyPaws.Application.Common.Authorization;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Patients;

/// <summary>
/// The live answer to "does this customer care for this pet?", read from
/// <c>pet_ownerships</c> on every request.
/// <para>
/// Not cached, and deliberately not carried in the access token. Care is revocable
/// (<c>DELETE /staff/pets/{id}/owners/{cid}</c>) and withdrawal usually follows a dispute — which
/// is exactly the moment a cached answer would keep working for up to fifteen minutes. The cost is
/// one indexed lookup per portal request.
/// </para>
/// <para>
/// This replaces the fail-closed stand-in that stood here while the Patients tables did not exist.
/// </para>
/// </summary>
internal sealed class PetAccessPolicy(HappyPawsDbContext db) : IPetAccessPolicy
{
    public Task<bool> IsCaredForByAsync(
        string petId,
        string customerId,
        CancellationToken cancellationToken = default) =>
        ActiveLinks(petId, customerId).AnyAsync(cancellationToken);

    public Task<bool> MayViewHistoryAsync(
        string petId,
        string customerId,
        CancellationToken cancellationToken = default) =>
        ActiveLinks(petId, customerId).AnyAsync(link => link.MayViewHistory, cancellationToken);

    public async Task<IReadOnlyCollection<string>> PetIdsForAsync(
        string customerId,
        CancellationToken cancellationToken = default) =>
        await db.Set<PetOwnership>()
            .AsNoTracking()
            .Where(link => link.CustomerId == customerId && link.RevokedAt == null)
            .Select(link => link.PetId)
            .ToListAsync(cancellationToken);

    private IQueryable<PetOwnership> ActiveLinks(string petId, string customerId) =>
        db.Set<PetOwnership>()
            .AsNoTracking()
            .Where(link => link.PetId == petId
                && link.CustomerId == customerId
                && link.RevokedAt == null);
}
