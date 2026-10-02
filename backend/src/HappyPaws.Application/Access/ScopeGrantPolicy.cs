using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Access;

/// <summary>
/// Nobody hands out a permission they do not hold themselves.
/// <para>
/// <c>role:manage</c> and <c>staff:manage</c> are permissions to <i>administer</i> access, and
/// without this rule they are quietly permissions to <i>have</i> any access: an admin could write a
/// role containing <c>*</c>, or assign themselves <c>clinic_owner</c>, and the contract's
/// distinction between an owner and an admin ("ทุกอย่างยกเว้น billing:payout, settings:danger")
/// would last exactly as long as nobody tried.
/// </para>
/// <para>
/// So every path that gives someone scopes — creating a role, rewriting one, inviting a member,
/// changing a member's roles — passes the scopes being given through here first. It applies in
/// both directions: taking <c>*</c> away from a role is refused for the same reason granting it
/// is, because stripping the owner's access is a way of becoming the most privileged account left.
/// </para>
/// <para>
/// A wildcard is a grant of scopes not yet invented, so holding every <c>invoice:</c> scope
/// individually does not entitle a caller to hand out <c>invoice:*</c>. <c>ScopeSet.Allows</c>
/// already reads it that way: a wildcard is allowed only by itself or by <c>*</c>.
/// </para>
/// </summary>
public sealed class ScopeGrantPolicy(ICurrentUser currentUser)
{
    /// <summary>
    /// Throws <c>403 insufficient_scope</c>, naming what the caller lacks, unless they hold every
    /// scope in <paramref name="scopes"/>.
    /// </summary>
    public void EnsureCallerHolds(IEnumerable<string> scopes)
    {
        var missing = currentUser.Scopes.MissingFrom(scopes.Distinct(StringComparer.Ordinal));

        if (missing.Count > 0)
        {
            throw new InsufficientScopeException(missing);
        }
    }
}
