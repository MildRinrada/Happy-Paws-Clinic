using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Auditing;

/// <summary>
/// Read and append port for the audit log. There is no update and no delete, here or anywhere
/// else — the table is append-only and the absence of a method is part of how that is enforced.
/// </summary>
public interface IAuditEntryRepository
{
    void Add(AuditEntry entry);

    /// <summary>
    /// <c>GET /staff/audit-logs</c> and <c>GET /staff/audit-logs/access</c>, newest first.
    /// <para>
    /// Filters are applied at the database rather than to a returned list: an audit log that
    /// shipped rows the caller may not see and then hid them in memory has already leaked them.
    /// </para>
    /// </summary>
    Task<CursorPage<AuditEntry>> ListAsync(
        AuditLogFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// What an audit reader can narrow by.
/// <para>
/// <see cref="VisibleBranchIds"/> is the branch restriction taken from the caller's token. It is
/// nullable rather than empty-by-default so that "unrestricted" is a deliberate value, not the
/// accidental result of forgetting to set it.
/// </para>
/// <para>
/// <see cref="AccessOnly"/> selects the reads — the access log the contract exposes separately.
/// </para>
/// </summary>
public sealed record AuditLogFilter(
    string? Action = null,
    string? ResourceType = null,
    string? ResourceId = null,
    string? PetId = null,
    string? ActorUserId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    bool AccessOnly = false,
    IReadOnlyCollection<string>? VisibleBranchIds = null);
