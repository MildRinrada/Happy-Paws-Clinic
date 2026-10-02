using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.Application.Prescriptions;

/// <summary>
/// A prescription with the two facts about its visit that decide who may act on it and who it
/// belongs to. A prescription carries neither itself; both come from the consultation it was written in.
/// </summary>
public sealed record PrescriptionInContext(Prescription Prescription, string BranchId, string CustomerId);

/// <summary>Persistence port for prescriptions read and dispensed outside the consultation that wrote them.</summary>
public interface IPrescriptionRepository
{
    /// <summary>Tracked, for dispensing.</summary>
    Task<PrescriptionInContext?> FindTrackedAsync(string prescriptionId, CancellationToken cancellationToken = default);

    /// <summary>Newest first.</summary>
    Task<CursorPage<Prescription>> ListAsync(
        PrescriptionFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>Every prescription for these patients, for checking one course against the others.</summary>
    Task<IReadOnlyList<Prescription>> ForPetsAsync(
        IReadOnlyCollection<string> petIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// What the prescription list may be narrowed by.
/// <para>
/// <c>Dispensed</c> is false for the pharmacy's outstanding work, true for what has left it, null
/// for both. <c>BranchIds</c> is the caller's branches, or null when unrestricted; it is applied
/// only to the clinic-wide list, because one patient's medication history is the whole history,
/// wherever each drug was written.
/// </para>
/// </summary>
public sealed record PrescriptionFilter(
    string? PetId = null,
    bool? Dispensed = null,
    IReadOnlyCollection<string>? BranchIds = null);
