using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.GetCalendarFeed;

public interface ICalendarFeedRepository
{
    void Add(CalendarFeed feed);

    Task<CalendarFeed?> FindByStaffAsync(string staffId, CancellationToken cancellationToken = default);

    Task<CalendarFeed?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Writes <c>last_used_at</c> directly; a calendar app polls, and a poll should not be a transaction.</summary>
    Task TouchAsync(string feedId, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appointments in a window for the feed, already reduced to what a calendar may show.
    /// <paramref name="staffId"/> narrows to one vet; <paramref name="branchIds"/> null means every branch.
    /// </summary>
    Task<IReadOnlyList<CalendarEntry>> AppointmentsAsync(
        string? staffId,
        IReadOnlyCollection<string>? branchIds,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// One appointment as a calendar shows it. Deliberately short: the service, the pet's name and the
/// branch. No customer name or phone, no reason for the visit, no note, nothing clinical.
/// </summary>
public sealed record CalendarEntry(
    string AppointmentId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    AppointmentStatus Status,
    string ServiceName,
    string PetName,
    string BranchName,
    DateTimeOffset Stamp);
