using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Common.Authorization;

/// <summary>
/// Resource-level gate for the customer portal: an account may only reach pets it cares for.
/// <para>
/// Every <c>/portal/*</c> use case that takes a pet id starts here. The refusal is deliberately
/// <c>404</c>, not <c>403</c>: answering "forbidden" for someone else's pet confirms that the id
/// exists, which turns the endpoint into an enumeration oracle over the clinic's patient list.
/// </para>
/// </summary>
public sealed class PetAccessGuard(ICurrentUser currentUser, IPetAccessPolicy policy)
{
    /// <summary>Throws <c>404</c> unless the caller cares for the pet.</summary>
    public async Task EnsureCaresForAsync(string petId, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        if (!await policy.IsCaredForByAsync(petId, customerId, cancellationToken))
        {
            throw new NotFoundException("pet", petId);
        }
    }

    /// <summary>
    /// Throws <c>404</c> unless the caller cares for the pet <b>and</b> holds history access on
    /// that link. Used by <c>GET /portal/pets/{id}/records</c>.
    /// </summary>
    public async Task EnsureMayViewHistoryAsync(string petId, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        if (!await policy.MayViewHistoryAsync(petId, customerId, cancellationToken))
        {
            throw new NotFoundException("pet", petId);
        }
    }

    /// <summary>The pet ids to restrict a portal list query to.</summary>
    public Task<IReadOnlyCollection<string>> VisiblePetIdsAsync(CancellationToken cancellationToken = default) =>
        policy.PetIdsForAsync(currentUser.RequireCustomerId(), cancellationToken);
}
