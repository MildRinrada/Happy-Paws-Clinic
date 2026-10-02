using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers;

/// <summary>
/// Consent history. Append and read; there is no update and no delete, because the history is the
/// point.
/// </summary>
public interface IConsentRepository
{
    void Add(CustomerConsent consent);

    /// <summary>Every consent row for a customer, newest first.</summary>
    Task<IReadOnlyList<CustomerConsent>> HistoryAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The current position on every purpose — the latest row per purpose, with defaults filled in
    /// for purposes never recorded.
    /// </summary>
    Task<IReadOnlyDictionary<ConsentPurpose, CustomerConsent?>> CurrentAsync(
        string customerId,
        CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for data-subject requests.</summary>
public interface IDataSubjectRequestRepository
{
    void Add(DataSubjectRequest request);

    Task<DataSubjectRequest?> FindByIdAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The clinic's queue of requests, oldest first — the opposite of every other list in this
    /// system, because here the oldest is the one closest to breaching a statutory deadline.
    /// </summary>
    Task<IReadOnlyList<DataSubjectRequest>> ListAsync(
        DataRequestStatus? status = null,
        string? customerId = null,
        CancellationToken cancellationToken = default);
}
