namespace HappyPaws.Application.Auditing;

/// <summary>
/// What a use case tells the audit trail. The actor, the address and the request id are
/// <b>not</b> here: they come from the ambient caller, so a handler cannot get them wrong and
/// cannot forge them.
/// </summary>
/// <param name="Action">A constant from <c>AuditActions</c>.</param>
/// <param name="ResourceType">A constant from <c>AuditResources</c>.</param>
/// <param name="ResourceId">The row acted on.</param>
/// <param name="PetId">The patient concerned, when there is one. Drives the per-patient access log.</param>
/// <param name="BranchId">Where it happened, when the resource has a branch.</param>
/// <param name="Details">Action-specific payload — a diff, a reason. Serialised to JSON.</param>
public sealed record AuditEvent(
    string Action,
    string ResourceType,
    string ResourceId,
    string? PetId = null,
    string? BranchId = null,
    IReadOnlyDictionary<string, object?>? Details = null);
