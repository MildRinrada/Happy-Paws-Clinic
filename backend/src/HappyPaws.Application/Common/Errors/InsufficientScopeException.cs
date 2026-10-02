namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// The caller does not hold the scopes an endpoint requires.
/// <para>
/// The contract requires the response to say <i>which</i> scopes are missing
/// ("พร้อมบอก scope ที่ขาด"), so they are carried in <c>details.missing_scopes</c>. This is safe to
/// disclose: scope names are already public through <c>GET /staff/permissions</c>, and telling an
/// integrator exactly what to request beats a blank 403.
/// </para>
/// </summary>
public sealed class InsufficientScopeException(IReadOnlyList<string> missingScopes)
    : AppException(ErrorCodes.InsufficientScope, "สิทธิ์ไม่เพียงพอสำหรับการดำเนินการนี้", HttpStatus.Forbidden)
{
    public IReadOnlyList<string> MissingScopes { get; } = missingScopes;
}
