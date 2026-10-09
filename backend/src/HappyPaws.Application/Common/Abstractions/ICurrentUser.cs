using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Access;

namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// The authenticated caller, resolved from the access token by the API layer.
/// <para>
/// Application services depend on this rather than on <c>HttpContext</c>, which keeps the layer
/// testable and free of ASP.NET Core types.
/// </para>
/// <para>
/// One account can hold both memberships at once — the contract is explicit that a vet may also be
/// a pet owner — so <see cref="StaffId"/> and <see cref="CustomerId"/> are independent nullables,
/// not two values of one enum.
/// </para>
/// </summary>
public interface ICurrentUser
{
    /// <summary>e.g. <c>usr_8fA2</c>. Null for public and webhook callers.</summary>
    string? UserId { get; }

    /// <summary>Staff membership id (<c>stf_204</c>) when this token carries one.</summary>
    string? StaffId { get; }

    /// <summary>Customer membership id (<c>cus_1180</c>) when this token carries one.</summary>
    string? CustomerId { get; }

    /// <summary>The revocable device session this token belongs to.</summary>
    string? SessionId { get; }

    /// <summary>Branch ids this staff membership is assigned to. Empty for customers.</summary>
    IReadOnlyCollection<string> BranchIds { get; }

    /// <summary>Effective scopes, already expanded from the caller's roles.</summary>
    ScopeSet Scopes { get; }

    /// <summary>True when this token was minted after a successful 2FA step.</summary>
    bool TwoFactorSatisfied { get; }

    bool IsAuthenticated { get; }

    /// <summary>
    /// The API key (<c>key_…</c>) this request authenticated with, or null for a person's token.
    /// A key acts as its creator, with its own narrower scopes and branches; this is how the audit
    /// log and the handlers that are for people only tell the difference.
    /// </summary>
    string? ApiKeyId => null;
}

/// <summary>
/// Guard helpers for the common membership checks, so use cases assert once instead of
/// hand-writing null checks that are easy to forget.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>The caller's staff id, or <c>403</c> when the token carries no staff membership.</summary>
    public static string RequireStaffId(this ICurrentUser user) =>
        user.StaffId ?? throw new ForbiddenException("caller has no staff membership");

    /// <summary>The caller's customer id, or <c>403</c> when the token carries no customer membership.</summary>
    public static string RequireCustomerId(this ICurrentUser user) =>
        user.CustomerId ?? throw new ForbiddenException("caller has no customer membership");

    /// <summary>Throws <c>403 insufficient_scope</c> naming everything missing, per the contract.</summary>
    public static void RequireScopes(this ICurrentUser user, params string[] requiredScopes)
    {
        var missing = user.Scopes.MissingFrom(requiredScopes);
        if (missing.Count > 0)
        {
            throw new InsufficientScopeException(missing);
        }
    }
}
