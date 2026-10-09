using HappyPaws.Application.Common.Pagination;
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

    /// <summary>
    /// One page of the list, oldest first (the order people are told in), with the pet's and the
    /// service's names. Untracked. See <see cref="WaitingListFilter"/> for what narrows it.
    /// </summary>
    Task<CursorPage<WaitingListRow>> ListAsync(
        WaitingListFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>The entry, tracked so it can be removed, or null.</summary>
    Task<WaitingListEntry?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entry when the unit of work commits.</summary>
    void Remove(WaitingListEntry entry);
}

/// <summary>
/// What <c>GET /staff/waiting-list</c> narrows by.
/// <para>
/// <c>BranchId</c> means "the entries an opening at this branch would reach": those that name the
/// branch and those that name none. <c>VisibleBranchIds</c> is the caller's branch restriction from
/// the token, applied the same way, so a branch-bound caller never pages past their branches.
/// </para>
/// </summary>
/// <param name="Today">The clinic day that decides which entries have lapsed.</param>
/// <param name="BranchId">Entries an opening at this branch would reach, or null for all.</param>
/// <param name="ServiceId">Only this service, or null for all.</param>
/// <param name="Lapsed">True for entries past their last day, false for those still waiting, null for both.</param>
/// <param name="VisibleBranchIds">The caller's branches, or null when not branch-bound.</param>
public sealed record WaitingListFilter(
    DateOnly Today,
    string? BranchId = null,
    string? ServiceId = null,
    bool? Lapsed = null,
    IReadOnlyCollection<string>? VisibleBranchIds = null);

/// <summary>An entry with the names the list shows beside it.</summary>
public sealed record WaitingListRow(WaitingListEntry Entry, string PetName, string ServiceName);
