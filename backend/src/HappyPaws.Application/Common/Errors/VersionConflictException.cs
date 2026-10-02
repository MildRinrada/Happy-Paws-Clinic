namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// The caller's <c>If-Match</c> did not match the resource's current <c>version</c> — someone else
/// changed the record since it was read.
/// <para>
/// This is the contract's optimistic concurrency answer, and it is what stops two receptionists
/// silently overwriting each other's edit to the same appointment. The client should re-read and
/// re-apply, not retry blindly, so the current version is returned in <c>details</c>.
/// </para>
/// </summary>
public sealed class VersionConflictException(string resource, uint expectedVersion, uint currentVersion)
    : AppException(ErrorCodes.VersionConflict, "ข้อมูลถูกแก้ไขโดยผู้อื่นแล้ว กรุณาโหลดใหม่", HttpStatus.Conflict)
{
    public string Resource { get; } = resource;

    /// <summary>The version the caller sent in <c>If-Match</c>.</summary>
    public uint ExpectedVersion { get; } = expectedVersion;

    /// <summary>The version actually stored now.</summary>
    public uint CurrentVersion { get; } = currentVersion;
}
