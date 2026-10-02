using System.Text.Json;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Auditing;

/// <summary>
/// Writes audit rows, filling in the actor and the request metadata from the ambient caller.
/// <para>
/// A use case says <i>what happened</i>; who did it, from where, and under which request id are
/// supplied here. That split is deliberate: a handler that had to pass its own actor could pass
/// the wrong one, and a handler that could pass an actor at all could pass someone else's.
/// </para>
/// </summary>
public sealed class AuditTrail(
    HappyPawsDbContext db,
    ICurrentUser currentUser,
    IRequestContext requestContext,
    IClock clock,
    ILogger<AuditTrail> logger) : IAuditTrail
{
    public void Record(AuditEvent auditEvent) => db.Set<AuditEntry>().Add(Build(auditEvent));

    public async Task RecordImmediatelyAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        db.Set<AuditEntry>().Add(Build(auditEvent));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A read that succeeded must not turn into a 500 because the log write failed — the
            // vet is mid-consultation and the record is already on screen. But an access log that
            // silently stops working is worse than one that never existed, so this is an error,
            // loudly, with everything needed to reconstruct the missing row.
            logger.LogError(
                exception,
                "Audit write failed — {Action} on {ResourceType} {ResourceId} by {UserId} was NOT recorded",
                auditEvent.Action,
                auditEvent.ResourceType,
                auditEvent.ResourceId,
                currentUser.UserId);
        }
    }

    private AuditEntry Build(AuditEvent auditEvent) =>
        AuditEntry.Write(
            auditEvent.Action,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            clock.UtcNow,
            actorUserId: currentUser.UserId,
            actorStaffId: currentUser.StaffId,
            actorCustomerId: currentUser.CustomerId,
            branchId: auditEvent.BranchId,
            petId: auditEvent.PetId,
            ipAddress: requestContext.IpAddress,
            userAgent: Truncate(requestContext.UserAgent, 400),
            requestId: requestContext.RequestId,
            detailsJson: Serialise(auditEvent.Details));

    private static string? Serialise(IReadOnlyDictionary<string, object?>? details) =>
        details is null || details.Count == 0 ? null : JsonSerializer.Serialize(details, Options);

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        // Thai field values appear in amendment diffs; escaping them would make the log unreadable
        // to the person it exists for.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
