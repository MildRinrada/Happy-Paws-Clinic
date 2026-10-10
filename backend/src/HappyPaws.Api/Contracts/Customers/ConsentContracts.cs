namespace HappyPaws.Api.Contracts.Customers;

/// <summary>
/// <c>PUT /portal/consents</c> — the customer moves their own toggles.
/// <para>
/// <c>consents</c> maps purpose to position: <c>service_messages</c>, <c>marketing</c>,
/// <c>medical_record_sharing</c>, <c>extended_data_processing</c>. Sending a subset is fine;
/// anything omitted is left where it is.
/// </para>
/// <para>
/// <c>policy_version</c> is accepted for older clients and ignored: the server stamps the label of
/// the privacy notice current when the row is written (<c>docs/contract-extensions.md § 35</c>).
/// </para>
/// </summary>
public sealed record UpdateConsentsRequest(
    IReadOnlyDictionary<string, bool> Consents,
    string? PolicyVersion = null);

/// <summary>
/// <c>POST /staff/customers/{id}/consents</c> — the counter records a decision taken on paper or
/// in person. <c>note</c> is where the paper form's reference goes. <c>policy_version</c> is
/// accepted and ignored, as on <see cref="UpdateConsentsRequest"/>.
/// </summary>
public sealed record RecordConsentRequest(
    IReadOnlyDictionary<string, bool> Consents,
    string? PolicyVersion = null,
    string? Note = null);

/// <summary>
/// <c>POST /portal/data-requests</c> — <c>type</c> is <c>access</c>, <c>correction</c>,
/// <c>erasure</c> or <c>portability</c>.
/// </summary>
public sealed record RaiseDataRequestBody(string Type, string? Detail = null);

/// <summary>
/// <c>POST /staff/data-requests/{id}/close</c> — <c>outcome</c> is <c>completed</c> or
/// <c>refused</c>, and <c>resolution</c> is required either way.
/// </summary>
public sealed record CloseDataRequestBody(string Outcome, string Resolution);
