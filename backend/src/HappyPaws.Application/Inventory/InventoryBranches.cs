using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Inventory;

/// <summary>
/// "คงเหลือรายสาขา" — stock belongs to a branch, and a member of staff works with the stock of the
/// branches they are assigned to, like every other staff read and write (<c>docs/authorization.md</c>).
/// <para>
/// A write names one branch, or falls back to the caller's only branch, or the clinic's only open
/// branch. Another branch's stock answers <c>403</c>. A read covers one branch when asked, otherwise
/// every branch the caller may see.
/// </para>
/// </summary>
public sealed class InventoryBranches(IBranchRepository branches, BranchGuard branchGuard)
{
    /// <summary>The branch a write lands in.</summary>
    public async Task<string> ForWriteAsync(string? requested, string field, CancellationToken cancellationToken = default)
    {
        if (requested is { Length: > 0 })
        {
            var branchId = requested.Trim();
            await EnsureOpenAsync(branchId, field, cancellationToken);
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
            : throw Invalid(field, "ต้องระบุสาขา");
    }

    /// <summary>The branches a read covers, in the clinic's own order.</summary>
    public async Task<IReadOnlyList<string>> ForReadAsync(string? requested, CancellationToken cancellationToken = default)
    {
        if (requested is { Length: > 0 })
        {
            var branchId = requested.Trim();
            await EnsureOpenAsync(branchId, "branch", cancellationToken);
            branchGuard.EnsureCanAccess(branchId);

            return [branchId];
        }

        var open = await branches.ListActiveAsync(cancellationToken);

        return [.. open.Select(branch => branch.Id).Where(branchGuard.CanAccess)];
    }

    /// <summary>A branch stock may be sent to: open, and not necessarily one the sender works at.</summary>
    public async Task EnsureOpenAsync(string branchId, string field, CancellationToken cancellationToken = default)
    {
        if ((await branches.MissingAsync([branchId], cancellationToken)).Count > 0)
        {
            throw Invalid(field, "ไม่พบสาขานี้");
        }
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
