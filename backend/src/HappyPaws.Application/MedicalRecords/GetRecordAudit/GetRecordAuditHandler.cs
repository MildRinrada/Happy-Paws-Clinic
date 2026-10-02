using HappyPaws.Application.Auditing;
using HappyPaws.Application.Auditing.Dtos;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.MedicalRecords.Dtos;
using HappyPaws.Application.MedicalRecords.GetMedicalRecord;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.MedicalRecords.GetRecordAudit;

/// <summary>
/// <c>GET /staff/medical-records/{id}/audit</c> —
/// "ใครแก้อะไรเมื่อไหร่ · diff รายฟิลด์".
/// </summary>
public sealed record GetRecordAuditQuery(string RecordId, int Limit = CursorPaging.DefaultLimit, string? Cursor = null);

/// <summary>One record's history: its amendments, and the audit rows that touched it.</summary>
public sealed record RecordAuditDto(
    string RecordId,
    string Status,
    DateTimeOffset SignedAt,
    string SignedByStaffId,
    bool IntegrityVerified,
    IReadOnlyList<AmendmentDto> Amendments,
    CursorPage<AuditEntryDto> Entries);

/// <summary>
/// The change history of one record, from two sources that answer different halves of the question.
/// <para>
/// The amendments say <i>what the document now says and how it got there</i>; the audit entries say
/// <i>who touched it, from where, and when</i> — including the views, which leave no trace in the
/// document itself. Returning only one of them would answer half the question a reviewer came with.
/// </para>
/// <para>
/// This route requires <c>record:audit</c>, a scope distinct from <c>record:read</c>. Reading a
/// patient's file and reviewing who else has read it are different powers, and the contract keeps
/// them apart.
/// </para>
/// </summary>
public sealed class GetRecordAuditHandler(
    IMedicalRecordRepository records,
    IAuditEntryRepository entries,
    BranchGuard branchGuard)
{
    public async Task<RecordAuditDto> HandleAsync(
        GetRecordAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        var record = await GetMedicalRecordHandler.Load(records, branchGuard, query.RecordId, cancellationToken);

        var page = await entries.ListAsync(
            new AuditLogFilter(
                ResourceType: AuditResources.MedicalRecord,
                ResourceId: record.Id,
                VisibleBranchIds: branchGuard.VisibleBranchIds),
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        return new RecordAuditDto(
            record.Id,
            MedicalRecordDto.ToWireStatus(record.Status),
            record.SignedAt,
            record.SignedByStaffId,
            record.VerifyIntegrity(),
            [.. record.Amendments.OrderBy(amendment => amendment.CreatedAt).Select(AmendmentDto.From)],
            new CursorPage<AuditEntryDto>([.. page.Data.Select(AuditEntryDto.From)], page.NextCursor));
    }
}
