using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Common.Authorization;

/// <summary>
/// Resource-level gate for staff: a member acts within the branches they are assigned to.
/// <para>
/// Holding <c>patient:read</c> does not mean reading every patient in the clinic — it means
/// reading patients at a branch you work at. The scope check cannot express that, because it is a
/// fact about the record, not about the token.
/// </para>
/// <para>
/// Branch assignments travel in the token (<c>"branches": ["brn_sukhumvit39"]</c>), so this needs
/// no database round-trip. Revocation is bounded by the 15-minute access-token lifetime, which is
/// the same window as every other token-carried fact.
/// </para>
/// <para>
/// Whether branch membership <i>constrains</i> access or merely defaults the filter is under-specified
/// in the contract; this takes the conservative reading. See <c>docs/assumptions.md § 9</c>.
/// </para>
/// </summary>
public sealed class BranchGuard(ICurrentUser currentUser)
{
    /// <summary>True when the caller may act on the given branch.</summary>
    public bool CanAccess(string branchId)
    {
        // An owner or admin holding `*` is not branch-bound; neither is a token with no branch
        // assignment at all, which is how single-branch clinics are represented.
        if (currentUser.Scopes.IsUnrestricted || currentUser.BranchIds.Count == 0)
        {
            return true;
        }

        return currentUser.BranchIds.Contains(branchId);
    }

    /// <summary>Throws <c>403</c> when the caller may not act on the given branch.</summary>
    public void EnsureCanAccess(string branchId)
    {
        if (!CanAccess(branchId))
        {
            throw new ForbiddenException($"staff is not assigned to branch {branchId}");
        }
    }

    /// <summary>
    /// The branches a list query should be restricted to, or null when the caller is unrestricted.
    /// Repositories apply this as a filter so a caller cannot page past their own branches.
    /// </summary>
    public IReadOnlyCollection<string>? VisibleBranchIds =>
        currentUser.Scopes.IsUnrestricted || currentUser.BranchIds.Count == 0
            ? null
            : currentUser.BranchIds;
}
