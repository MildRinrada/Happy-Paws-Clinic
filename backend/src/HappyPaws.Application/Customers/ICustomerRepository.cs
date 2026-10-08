using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers;

/// <summary>
/// Persistence port for customer records.
/// <para>
/// Reads come back untracked and writes take a tracked lookup, kept as two methods for the same
/// reason <c>IPetRepository</c> does: most callers here are read-only, and a repository that always
/// tracked would let a distant handler mutate a customer it only meant to display.
/// </para>
/// </summary>
public interface ICustomerRepository
{
    void Add(Customer customer);

    Task<Customer?> FindByIdAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>The same lookup, tracked, for the handlers that change something.</summary>
    Task<Customer?> FindTrackedByIdAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The record carrying this LINE address, merged ones included, since the unique index covers
    /// them too. For the link's "already someone else's" answer (<c>docs/contract-extensions.md § 18</c>).
    /// </summary>
    Task<Customer?> WithLineAddressAsync(string lineUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /staff/customers?q=</c> — "ค้นด้วยชื่อ เบอร์ HN หรือชื่อสัตว์".
    /// <para>
    /// One query across four things a counter might be told: the person's name, their phone, a
    /// patient's hospital number, or a patient's name. Merged records never appear.
    /// </para>
    /// </summary>
    Task<CursorPage<Customer>> SearchAsync(
        string? query,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Live records sharing a phone number — the counter's duplicate check
    /// ("ตรวจซ้ำด้วยเบอร์โทร"). A warning, never a refusal: a household shares one number.
    /// </summary>
    Task<IReadOnlyList<Customer>> WithPhoneAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Live records with no portal account whose phone, read as digits only, is this number —
    /// tracked, oldest first. How a sign-up finds the record the counter already made, whether the
    /// counter typed <c>081-234-5678</c> or <c>0812345678</c>.
    /// </summary>
    Task<IReadOnlyList<Customer>> UnlinkedWithPhoneDigitsAsync(string phoneDigits, CancellationToken cancellationToken = default);

    /// <summary>Moves every row that points at <paramref name="duplicateId"/> onto the survivor.</summary>
    Task<IReadOnlyDictionary<string, int>> ReassignAsync(
        string duplicateId,
        string survivingId,
        CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for portal invitations.</summary>
public interface ICustomerInvitationRepository
{
    void Add(CustomerInvitation invitation);

    /// <summary>The customer's most recent invitation, tracked, or null if never invited.</summary>
    Task<CustomerInvitation?> LatestForCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default);
}
