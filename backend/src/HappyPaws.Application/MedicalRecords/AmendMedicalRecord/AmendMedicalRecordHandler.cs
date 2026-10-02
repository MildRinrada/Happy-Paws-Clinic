using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.MedicalRecords.Dtos;
using HappyPaws.Application.MedicalRecords.GetMedicalRecord;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.MedicalRecords;

namespace HappyPaws.Application.MedicalRecords.AmendMedicalRecord;

/// <summary>
/// <c>POST /staff/medical-records/{id}/amendments</c> —
/// "แก้ไขแบบมีร่องรอย · เก็บฉบับเดิมไว้เสมอ".
/// <para>
/// <c>Changes</c> maps field name to new value, over the narrative fields only. See
/// <see cref="RecordContent.AmendableFields"/> for why the structured parts are excluded.
/// </para>
/// </summary>
public sealed record AmendMedicalRecordCommand(
    string RecordId,
    string Reason,
    IReadOnlyDictionary<string, string?> Changes,
    uint? IfMatchVersion = null);

/// <summary>
/// Corrects a signed record without erasing anything.
/// <para>
/// The original document is never touched. An amendment stores the full corrected version
/// alongside a field-level diff, and the record's status moves to <c>amended</c> — so what the
/// clinic said at the time, what it says now, and who changed it in between are all readable years
/// later. That is the whole point of "แก้ไขแบบมีร่องรอย".
/// </para>
/// <para>
/// <c>If-Match</c> is honoured but not required here, in line with every other mutation in this
/// codebase. Two vets amending the same record concurrently would each be appending their own
/// version, and the second one's diff is computed against whatever it actually read — the history
/// stays coherent either way.
/// </para>
/// </summary>
public sealed class AmendMedicalRecordHandler(
    IMedicalRecordRepository records,
    IAuditTrail audit,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<MedicalRecordDto> HandleAsync(
        AmendMedicalRecordCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        MedicalRecord? amended = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var record = await GetMedicalRecordHandler.Load(records, branchGuard, command.RecordId, token);

            ConcurrencyGuard.Ensure(record, command.IfMatchVersion);

            var (content, changes) = RecordContent.Apply(record.CurrentContentJson, command.Changes);

            var amendment = record.Amend(content, changes, command.Reason, staffId, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.RecordAmended,
                AuditResources.MedicalRecord,
                record.Id,
                record.PetId,
                record.BranchId,
                new Dictionary<string, object?>
                {
                    ["amendment_id"] = amendment.Id,
                    ["reason"] = amendment.Reason,
                    // The same diff the amendment stores, copied into the log so that
                    // "ใครแก้อะไรเมื่อไหร่" is answerable from the audit table alone.
                    ["changes"] = System.Text.Json.JsonDocument.Parse(changes).RootElement,
                }));

            amended = record;
        }, cancellationToken);

        return MedicalRecordDto.From(amended!);
    }
}
