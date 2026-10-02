namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// The caller is authenticated and holds the endpoint's scope, but is not allowed to act on
/// <b>this particular record</b> — a vet reaching outside their assigned branches, an owner
/// touching a pet they do not care for.
/// <para>
/// This is the resource-level gate. Scope-level refusals are
/// <see cref="InsufficientScopeException"/>; hidden-existence refusals are
/// <see cref="NotFoundException"/>.
/// </para>
/// </summary>
public sealed class ForbiddenException(string reason)
    : AppException(ErrorCodes.Forbidden, "ไม่มีสิทธิ์เข้าถึงข้อมูลนี้", HttpStatus.Forbidden)
{
    /// <summary>Why access was refused. Logged and audited; never returned to the caller.</summary>
    public string Reason { get; } = reason;
}
