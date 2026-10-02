namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// The requested resource does not exist — or does exist, but the caller has no business knowing
/// that it does.
/// <para>
/// The second case matters. When a customer asks for another owner's appointment, answering
/// <c>403</c> confirms the id is real. Throw this instead, so existence itself stays private.
/// Reserve <see cref="ForbiddenException"/> for callers already entitled to know the resource
/// exists.
/// </para>
/// </summary>
public sealed class NotFoundException(string resource, string resourceId)
    : AppException(ErrorCodes.NotFound, $"ไม่พบข้อมูลที่ระบุ ({resource})", HttpStatus.NotFound)
{
    public string Resource { get; } = resource;

    /// <summary>Kept for logging and audit. Deliberately not echoed back to the caller.</summary>
    public string ResourceId { get; } = resourceId;
}
