using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients;

/// <summary>
/// Persistence port for patients.
/// <para>
/// Declared minimal for booking before the Patients module existed; now the module's own. The
/// tracked/untracked split stays, because the many read-only callers — booking, the queue, the
/// allergy check — should not silently take a change-tracked entity.
/// </para>
/// </summary>
public interface IPetRepository
{
    void Add(Pet pet);

    Task<Pet?> FindByIdAsync(string petId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The same lookup, tracked for modification. Kept separate from
    /// <see cref="FindByIdAsync"/> so that the many read-only callers — booking, the queue, the
    /// allergy check — stay read-only rather than silently taking a change-tracked entity.
    /// </summary>
    Task<Pet?> FindTrackedByIdAsync(string petId, CancellationToken cancellationToken = default);

    /// <summary><c>GET /staff/pets</c> — "ทะเบียนคนไข้ทั้งคลินิก", with the shared <c>?q=</c> filter.</summary>
    Task<CursorPage<Pet>> ListAsync(
        PetListFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>Every live patient of a customer — the "สัตว์ทั้งหมด" on a customer record.</summary>
    Task<IReadOnlyList<Pet>> ForCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>True when the chip number is already on another patient's record.</summary>
    Task<bool> MicrochipExistsAsync(string microchipNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// What the patient registry may be narrowed by.
/// </summary>
/// <param name="Query">Name, hospital number or microchip — the three things printed on a collar tag.</param>
/// <param name="CustomerId">One owner's patients.</param>
/// <param name="Species">"หมา", "แมว" — free text, because the contract defines no species list.</param>
/// <param name="IncludeDeceased">
/// False by default. A registry that shows every patient who ever lived is not the list a
/// receptionist means by "ทะเบียนคนไข้", and the closed records are still reachable by id.
/// </param>
public sealed record PetListFilter(
    string? Query = null,
    string? CustomerId = null,
    string? Species = null,
    bool IncludeDeceased = false);

/// <summary>Persistence port for the carer links that decide what a portal account may reach.</summary>
public interface IPetOwnershipRepository
{
    void Add(PetOwnership ownership);

    /// <summary>
    /// Deletes a link outright. The <b>only</b> caller is the customer merge, folding a duplicate
    /// link into the survivor's — everywhere else a link is closed with <c>Revoke</c>, because
    /// "ถอนสิทธิ์เข้าถึง ไม่ลบประวัติ" means the row stays and says until when.
    /// </summary>
    void Remove(PetOwnership ownership);

    /// <summary>Live and revoked links for a patient, tracked — the carer list on a pet record.</summary>
    Task<IReadOnlyList<PetOwnership>> ForPetAsync(string petId, CancellationToken cancellationToken = default);

    /// <summary>Every link a customer holds, tracked. Used by the merge.</summary>
    Task<IReadOnlyList<PetOwnership>> ForCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>The live link between one patient and one carer, tracked, or null.</summary>
    Task<PetOwnership?> FindActiveAsync(
        string petId,
        string customerId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Issues the next hospital number — "ออก HN อัตโนมัติ".
/// <para>
/// Its own port for the reason the queue's and the invoice's are: "next number" is a concurrency
/// problem, not a domain rule, and two patients registered in the same second must not be handed
/// the same HN. Must be called inside the caller's transaction.
/// </para>
/// </summary>
public interface IHospitalNumberGenerator
{
    /// <summary>e.g. <c>HN-2026-0042</c>.</summary>
    Task<string> NextAsync(DateTimeOffset registeredAt, CancellationToken cancellationToken = default);
}
