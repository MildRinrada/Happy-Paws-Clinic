using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Auditing;

/// <summary>
/// One line in the audit log — "ใคร ทำอะไร กับเรกคอร์ดไหน เมื่อไหร่ จาก IP ใด".
/// <para>
/// Append-only by construction: every property is set once by <see cref="Write"/> and there is no
/// mutator on this type at all. An audit trail that can be edited is not evidence of anything, and
/// the one situation these rows exist for — a dispute about who saw or changed a patient's file —
/// is exactly the situation in which someone would want to edit them.
/// </para>
/// <para>
/// <see cref="PetId"/> is carried separately from <see cref="ResourceId"/> even when the resource
/// is a record belonging to that pet. The access log is read <i>per patient</i>, and resolving a
/// record id back to its pet at read time would mean joining through data that may itself have
/// been amended since.
/// </para>
/// </summary>
public sealed class AuditEntry : Entity
{
    private AuditEntry()
    {
    }

    /// <summary>A value from <see cref="AuditActions"/>. Never free text.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>A value from <see cref="AuditResources"/>.</summary>
    public string ResourceType { get; private set; } = string.Empty;

    public string ResourceId { get; private set; } = string.Empty;

    /// <summary>Null only for actions taken before anyone was authenticated — a failed sign-in.</summary>
    public string? ActorUserId { get; private set; }

    public string? ActorStaffId { get; private set; }

    public string? ActorCustomerId { get; private set; }

    public string? BranchId { get; private set; }

    /// <summary>The patient this row concerns, when there is one.</summary>
    public string? PetId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    /// <summary>The <c>request_id</c> echoed to the caller, so a support ticket joins to this row.</summary>
    public string? RequestId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Action-specific payload as JSON — the field-level diff of an amendment, the reason a queue
    /// priority was raised. Opaque to the domain; rendered by the reader.
    /// </summary>
    public string? DetailsJson { get; private set; }

    /// <summary>True when this row belongs in the access log rather than the change log.</summary>
    public bool IsAccess => AuditActions.IsAccess(Action);

    public static AuditEntry Write(
        string action,
        string resourceType,
        string resourceId,
        DateTimeOffset occurredAt,
        string? actorUserId = null,
        string? actorStaffId = null,
        string? actorCustomerId = null,
        string? branchId = null,
        string? petId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestId = null,
        string? detailsJson = null)
    {
        if (!AuditActions.IsKnown(action))
        {
            // A typo'd action name would silently create a category nobody queries. The list in
            // AuditActions is the contract; writing outside it is a bug, not a new kind of event.
            throw new DomainException("unknown_audit_action", $"ไม่รู้จัก audit action: {action}");
        }

        return new AuditEntry
        {
            Id = EntityId.New(EntityId.AuditEntry),
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            OccurredAt = occurredAt,
            ActorUserId = actorUserId,
            ActorStaffId = actorStaffId,
            ActorCustomerId = actorCustomerId,
            BranchId = branchId,
            PetId = petId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestId = requestId,
            DetailsJson = detailsJson,
        };
    }
}
