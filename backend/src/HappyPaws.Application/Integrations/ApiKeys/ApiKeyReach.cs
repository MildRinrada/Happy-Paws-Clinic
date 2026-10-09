using HappyPaws.Application.Common.Authorization;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.ApiKeys;

/// <summary>
/// Which keys a caller may list and revoke (<c>docs/contract-extensions.md § 22</c>): the keys they
/// could have made. A caller bound to no branch reaches every key. A branch-bound caller reaches a
/// key whose branches are all theirs; a key for other branches, or for every branch, is not theirs
/// to see or stop, and answers <c>404</c> as though it did not exist.
/// </summary>
public sealed class ApiKeyReach(BranchGuard branchGuard)
{
    public bool Allows(ApiKey key) =>
        branchGuard.VisibleBranchIds is not { } own
        || (key.BranchIds.Count > 0 && key.BranchIds.All(own.Contains));
}
