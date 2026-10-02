using HappyPaws.Application.Auditing.Dtos;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.Application.Auditing.ListAuditLogs;

/// <summary>
/// <c>GET /staff/audit-logs</c> and <c>GET /staff/audit-logs/access</c>.
/// <para>
/// One query type for both endpoints, distinguished by <c>AccessOnly</c>. They differ by a filter,
/// not by behaviour, and separate handlers would mean maintaining the branch restriction twice —
/// the sort of duplication that leaks a log the second time it is edited.
/// </para>
/// </summary>
public sealed record ListAuditLogsQuery(
    bool AccessOnly = false,
    string? Action = null,
    string? ResourceType = null,
    string? ResourceId = null,
    string? PetId = null,
    string? ActorUserId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// Reads the audit log for a staff caller holding <c>audit:read</c>.
/// <para>
/// The log is still branch-scoped. Holding <c>audit:read</c> answers "may this person read audit
/// data", not "may this person read every site's audit data" — and a multi-branch clinic's audit
/// log is precisely where that distinction matters. Rows carrying no branch (a sign-in, a consent
/// change) belong to no site and stay visible to any auditor.
/// </para>
/// </summary>
public sealed class ListAuditLogsHandler(IAuditEntryRepository entries, BranchGuard branchGuard)
{
    public async Task<CursorPage<AuditEntryDto>> HandleAsync(
        ListAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilter(
            Action: query.Action,
            ResourceType: query.ResourceType,
            ResourceId: query.ResourceId,
            PetId: query.PetId,
            ActorUserId: query.ActorUserId,
            From: query.From,
            To: query.To,
            AccessOnly: query.AccessOnly,
            VisibleBranchIds: branchGuard.VisibleBranchIds);

        var page = await entries.ListAsync(
            filter,
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        return new CursorPage<AuditEntryDto>([.. page.Data.Select(AuditEntryDto.From)], page.NextCursor);
    }
}
