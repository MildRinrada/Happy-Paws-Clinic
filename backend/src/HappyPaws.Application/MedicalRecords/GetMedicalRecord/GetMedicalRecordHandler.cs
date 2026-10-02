using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.MedicalRecords.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.MedicalRecords;

namespace HappyPaws.Application.MedicalRecords.GetMedicalRecord;

/// <summary>
/// <c>GET /staff/medical-records/{id}</c> — "ฉบับที่ sign แล้วพร้อมลายเซ็นและเวลา".
/// </summary>
public sealed record GetMedicalRecordQuery(string RecordId);

/// <summary>
/// Opens a signed record — and writes down that it was opened.
/// <para>
/// The audit write is the reason this handler exists at all rather than the controller reading the
/// repository. <c>docs/authorization.md</c> commits to recording every view of a medical record and
/// <c>GET /staff/audit-logs/access</c> exists to read those rows back; a read path that skipped the
/// write would make both a lie. This is the single most important row the audit table holds,
/// because it is the one a patient's owner is entitled to ask about.
/// </para>
/// <para>
/// The log write happens <b>after</b> authorization and only on success. Recording refused attempts
/// here would fill the access log with rows that describe no access; a rejected read is a security
/// event and belongs in the security log, which the 403 already produces.
/// </para>
/// </summary>
public sealed class GetMedicalRecordHandler(
    IMedicalRecordRepository records,
    IAuditTrail audit,
    BranchGuard branchGuard)
{
    public async Task<MedicalRecordDto> HandleAsync(
        GetMedicalRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        var record = await Load(records, branchGuard, query.RecordId, cancellationToken);

        await audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.RecordViewed,
                AuditResources.MedicalRecord,
                record.Id,
                record.PetId,
                record.BranchId),
            cancellationToken);

        return MedicalRecordDto.From(record);
    }

    /// <summary>
    /// Loads a record and applies the branch gate. Shared with the amendment and audit use cases so
    /// all three agree on who may reach a record — three copies of this check would be three chances
    /// to get it wrong.
    /// </summary>
    internal static async Task<MedicalRecord> Load(
        IMedicalRecordRepository records,
        BranchGuard branchGuard,
        string recordId,
        CancellationToken cancellationToken)
    {
        var record = await records.FindByIdAsync(recordId, cancellationToken)
            ?? throw new NotFoundException("medical record", recordId);

        branchGuard.EnsureCanAccess(record.BranchId);

        return record;
    }
}
