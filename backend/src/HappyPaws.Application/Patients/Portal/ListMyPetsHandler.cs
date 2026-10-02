using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Application.Patients.Portal;

/// <summary>
/// <c>GET /portal/pets</c> — "สัตว์ทั้งหมดของบัญชีนี้".
/// <para>
/// Every pet the caller cares for through a live carer link, whether they are the primary owner or
/// a co-owner; a link that was withdrawn takes the pet off this list at once. Pets that have died
/// stay on it, marked: an owner looking for an old record should still find it.
/// </para>
/// </summary>
public sealed class ListMyPetsHandler(IPetRepository pets, MyPetReader reader, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<MyPetDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();
        var mine = await pets.ForCustomerAsync(customerId, cancellationToken);

        var result = new List<MyPetDto>(mine.Count);

        foreach (var pet in mine)
        {
            result.Add(await reader.ReadAsync(pet, customerId, cancellationToken));
        }

        return result;
    }
}
