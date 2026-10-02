using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Declares the scope an endpoint needs, mirroring the "Scope ที่ต้องใช้" column of the API reference:
/// <code>[RequireScope(Scopes.Appointment.Write)]</code>
/// <para>
/// The policy name is the scope string itself, resolved on demand by the scope policy provider,
/// so adding an endpoint never means editing a central policy registry.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireScopeAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "scope:";

    public RequireScopeAttribute(string scope)
    {
        Scope = scope;
        Policy = PolicyPrefix + scope;
    }

    public string Scope { get; }
}
