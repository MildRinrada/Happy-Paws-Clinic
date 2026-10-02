using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics;

/// <summary>Persistence port for the clinic's sites.</summary>
public interface IBranchRepository
{
    void Add(Branch branch);

    /// <summary>Open branches in a stable order — what <c>GET /clinic/branches</c> shows the public.</summary>
    Task<IReadOnlyList<Branch>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// The ids in <paramref name="branchIds"/> that are not an open branch of this clinic. Empty
    /// when all of them are — which is how staff, invitations and services check what they were
    /// told to point at before storing it.
    /// </summary>
    Task<IReadOnlyList<string>> MissingAsync(
        IReadOnlyCollection<string> branchIds,
        CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for the days the clinic is shut.</summary>
public interface IHolidayRepository
{
    void Add(ClinicHoliday holiday);

    /// <summary>True when that exact closure — same day, same branch or the same "every branch" — is already declared.</summary>
    Task<bool> ExistsAsync(DateOnly date, string? branchId, CancellationToken cancellationToken = default);

    /// <summary>Holidays on or after a day, soonest first.</summary>
    Task<IReadOnlyList<ClinicHoliday>> FromAsync(
        DateOnly from,
        int take,
        CancellationToken cancellationToken = default);
}
