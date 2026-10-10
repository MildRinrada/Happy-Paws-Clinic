using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Forecasting;

/// <summary>
/// The branch a forecast is about. Stock and rosters belong to one branch, and "how long will this
/// last" or "how many vets are in at 10:00" has no answer summed over several, so the two forecasts
/// that need one branch resolve it here (<c>docs/contract-extensions.md § 33</c>).
/// <para>
/// The same rule as a stock write (<c>InventoryBranches.ForWriteAsync</c>): the branch asked for,
/// checked against the caller (<c>403</c> for one they do not work at, <c>422</c> for one that is
/// not open); otherwise the caller's only branch; otherwise the clinic's only open branch;
/// otherwise <c>422</c> asking for one.
/// </para>
/// </summary>
public sealed class ForecastBranches(IBranchRepository branches, BranchGuard branchGuard)
{
    public async Task<string> OneAsync(string? requested, CancellationToken cancellationToken = default)
    {
        if (requested is { Length: > 0 })
        {
            var branchId = requested.Trim();

            if ((await branches.MissingAsync([branchId], cancellationToken)).Count > 0)
            {
                throw Invalid("ไม่พบสาขานี้");
            }

            branchGuard.EnsureCanAccess(branchId);

            return branchId;
        }

        if (branchGuard.VisibleBranchIds is { Count: 1 } mine)
        {
            return mine.First();
        }

        var open = await branches.ListActiveAsync(cancellationToken);

        return open.Count == 1 && branchGuard.CanAccess(open[0].Id)
            ? open[0].Id
            : throw Invalid("ต้องระบุสาขา");
    }

    /// <summary>
    /// The branches a list covers: the one asked for (checked against the caller), or every branch
    /// the caller may see, which is null for a caller the branch guard does not restrict.
    /// </summary>
    public IReadOnlyCollection<string>? Scope(string? requested)
    {
        if (requested is { Length: > 0 })
        {
            var branchId = requested.Trim();
            branchGuard.EnsureCanAccess(branchId);

            return [branchId];
        }

        return branchGuard.VisibleBranchIds;
    }

    private static ValidationFailedException Invalid(string message) =>
        new(new Dictionary<string, string[]> { ["branch"] = [message] });
}
