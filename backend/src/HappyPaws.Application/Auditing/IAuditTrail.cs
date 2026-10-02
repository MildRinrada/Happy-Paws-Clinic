namespace HappyPaws.Application.Auditing;

/// <summary>
/// Where audited actions are written.
/// <para>
/// Two methods because there are two situations, and conflating them loses one of them.
/// </para>
/// <para>
/// <see cref="Record"/> enlists the entry in whatever unit of work is already open, so the audit
/// row and the change it describes commit together. If the operation rolls back, so does its audit
/// row — which is correct: nothing happened, so nothing should be claimed to have happened.
/// </para>
/// <para>
/// <see cref="RecordImmediatelyAsync"/> exists for <b>reads</b>. Opening a medical record changes
/// nothing, so there is no transaction to join, yet the view is the single most important thing
/// this log captures. It writes and saves on its own.
/// </para>
/// </summary>
public interface IAuditTrail
{
    /// <summary>Enlists an entry in the current unit of work. Call inside the transaction.</summary>
    void Record(AuditEvent auditEvent);

    /// <summary>Writes and commits one entry on its own — for read paths with nothing else to save.</summary>
    Task RecordImmediatelyAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
