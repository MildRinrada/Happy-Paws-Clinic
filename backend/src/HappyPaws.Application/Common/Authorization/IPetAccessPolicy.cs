namespace HappyPaws.Application.Common.Authorization;

/// <summary>
/// Answers "does this customer care for this pet?" — the resource-level rule behind every
/// <c>/portal/*</c> endpoint.
/// <para>
/// This has to hit the database. Pet care is many-to-many and revocable
/// (<c>POST</c>/<c>DELETE /staff/pets/{id}/owners</c>), so it must be a live query. Caching the
/// answer in the token would keep working after a co-owner's access was withdrawn — which is
/// precisely the case where it must stop working, since withdrawal usually follows a dispute.
/// </para>
/// <para>
/// Implemented in <c>Infrastructure/Persistence/Repositories/Patients/</c> once the Patients module
/// lands. Declared here so the guard and the use cases that depend on it can be written and tested
/// against a fake first.
/// </para>
/// </summary>
public interface IPetAccessPolicy
{
    /// <summary>True when the customer is a current owner or co-owner of the pet.</summary>
    Task<bool> IsCaredForByAsync(string petId, string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the customer is additionally permitted to view the pet's clinical history.
    /// The owner link carries its own "may view history" flag, so a co-owner can be given booking
    /// rights without access to medical records.
    /// </summary>
    Task<bool> MayViewHistoryAsync(string petId, string customerId, CancellationToken cancellationToken = default);

    /// <summary>The pet ids this customer cares for, used to scope list queries at the database.</summary>
    Task<IReadOnlyCollection<string>> PetIdsForAsync(string customerId, CancellationToken cancellationToken = default);
}
