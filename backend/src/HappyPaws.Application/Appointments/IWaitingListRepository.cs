using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments;

/// <summary>Persistence port for the waiting list.</summary>
public interface IWaitingListRepository
{
    void Add(WaitingListEntry entry);

    /// <summary>True when this pet is already waiting for this service with a window that has not passed.</summary>
    Task<bool> IsWaitingAsync(
        string petId,
        string serviceId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Entries for a service whose window includes a day, oldest first and tracked, so the people
    /// who joined first are told first.
    /// </summary>
    Task<IReadOnlyList<WaitingListEntry>> WaitingForAsync(
        string serviceId,
        DateOnly on,
        CancellationToken cancellationToken = default);
}
