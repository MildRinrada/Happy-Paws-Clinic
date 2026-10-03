using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics;

/// <summary>Persistence port for the clinic's contact entries (contract extension § 15).</summary>
public interface IContactRepository
{
    void Add(ClinicContact contact);

    void Remove(ClinicContact contact);

    /// <summary>One entry, tracked for change.</summary>
    Task<ClinicContact?> FindTrackedAsync(string contactId, CancellationToken cancellationToken = default);

    /// <summary>Every entry in display order: by <c>sort_order</c>, then the order they were added.</summary>
    Task<IReadOnlyList<ClinicContact>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
