using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Customers;

/// <summary>The rights a data subject can exercise.</summary>
public enum DataRequestType
{
    /// <summary>"Show me everything you hold about me."</summary>
    Access,

    /// <summary>"Correct what is wrong."</summary>
    Correction,

    /// <summary>"Delete it."</summary>
    Erasure,

    /// <summary>"Give it to me in a form I can take elsewhere."</summary>
    Portability,
}

public enum DataRequestStatus
{
    Open,
    Completed,

    /// <summary>Refused with a stated reason — which is itself a lawful outcome, and is recorded.</summary>
    Refused,
}

/// <summary>
/// A request from a customer to exercise a data-protection right.
/// <para>
/// <b>Deliberately a workflow record, not an automatic action.</b> Nothing here deletes anything.
/// An erasure request against a veterinary clinic collides with record-retention duties — a signed
/// medical record is evidence of treatment given to a living animal, and this system's own rule is
/// that it is never deleted. Reconciling the two is a judgement a human has to make and be
/// accountable for, so what the API guarantees is that the request is <i>received, timed and
/// answered</i>, which is the part that is currently impossible.
/// </para>
/// <para>
/// Not in the API reference — see <c>docs/contract-extensions.md § 4</c>.
/// </para>
/// </summary>
public sealed class DataSubjectRequest : Entity
{
    private DataSubjectRequest()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    public DataRequestType Type { get; private set; }

    public DataRequestStatus Status { get; private set; } = DataRequestStatus.Open;

    public DateTimeOffset RaisedAt { get; private set; }

    /// <summary>What the customer asked for, in their words.</summary>
    public string? Detail { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public string? ClosedByUserId { get; private set; }

    /// <summary>What the clinic did, or why it declined. Required to close.</summary>
    public string? Resolution { get; private set; }

    /// <summary>How long the request has been open — the number a regulator asks for.</summary>
    public TimeSpan AgeAt(DateTimeOffset now) => (ClosedAt ?? now) - RaisedAt;

    public static DataSubjectRequest Raise(
        string customerId,
        DataRequestType type,
        DateTimeOffset raisedAt,
        string? detail = null) =>
        new()
        {
            Id = EntityId.New(EntityId.DataRequest),
            CustomerId = customerId,
            Type = type,
            RaisedAt = raisedAt,
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim(),
        };

    public void Close(DataRequestStatus outcome, string byUserId, string resolution, DateTimeOffset at)
    {
        if (Status != DataRequestStatus.Open)
        {
            throw new StateConflictException(nameof(DataSubjectRequest), Status.ToString(), nameof(Close));
        }

        if (outcome is not (DataRequestStatus.Completed or DataRequestStatus.Refused))
        {
            throw new DomainException("invalid_data_request", "ผลการดำเนินการต้องเป็น completed หรือ refused");
        }

        if (string.IsNullOrWhiteSpace(resolution))
        {
            // A closed request with no stated outcome is indistinguishable from an ignored one,
            // and "we answered it" is exactly the claim that will need evidence later.
            throw new DomainException("invalid_data_request", "ต้องระบุผลการดำเนินการ");
        }

        Status = outcome;
        ClosedAt = at;
        ClosedByUserId = byUserId;
        Resolution = resolution.Trim();
    }
}
