using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers.Dtos;

/// <summary>
/// Where a customer stands on one purpose right now.
/// <para>
/// <c>RecordedAt</c> is null when the position is the default and nobody has ever been asked —
/// which is itself the honest answer, and different from "they said yes".
/// </para>
/// </summary>
public sealed record ConsentStateDto(
    string Purpose,
    bool Granted,
    bool IsDefault,
    DateTimeOffset? RecordedAt,
    string? Source,
    string? PolicyVersion)
{
    public static ConsentStateDto From(ConsentPurpose purpose, CustomerConsent? recorded) =>
        recorded is null
            ? new ConsentStateDto(
                ToWire(purpose), CustomerConsent.DefaultFor(purpose), true, null, null, null)
            : new ConsentStateDto(
                ToWire(purpose),
                recorded.Granted,
                false,
                recorded.RecordedAt,
                recorded.Source.ToString().ToLowerInvariant(),
                recorded.PolicyVersion);

    /// <summary><c>ServiceMessages</c> → <c>service_messages</c>.</summary>
    public static string ToWire(ConsentPurpose purpose) => purpose switch
    {
        ConsentPurpose.ServiceMessages => "service_messages",
        ConsentPurpose.Marketing => "marketing",
        ConsentPurpose.MedicalRecordSharing => "medical_record_sharing",
        ConsentPurpose.ExtendedDataProcessing => "extended_data_processing",
        _ => purpose.ToString().ToLowerInvariant(),
    };

    public static ConsentPurpose? ParseWire(string value) => value switch
    {
        "service_messages" => ConsentPurpose.ServiceMessages,
        "marketing" => ConsentPurpose.Marketing,
        "medical_record_sharing" => ConsentPurpose.MedicalRecordSharing,
        "extended_data_processing" => ConsentPurpose.ExtendedDataProcessing,
        _ => null,
    };
}

/// <summary>The whole picture for one customer: where they stand, and how they got there.</summary>
public sealed record ConsentSummaryDto(
    string CustomerId,
    IReadOnlyList<ConsentStateDto> Consents,
    IReadOnlyList<ConsentHistoryDto> History);

/// <summary>One past decision. The audit trail a complaint is answered from.</summary>
public sealed record ConsentHistoryDto(
    string Id,
    string Purpose,
    bool Granted,
    DateTimeOffset RecordedAt,
    string Source,
    string? RecordedByUserId,
    string? Note)
{
    public static ConsentHistoryDto From(CustomerConsent consent) =>
        new(
            consent.Id,
            ConsentStateDto.ToWire(consent.Purpose),
            consent.Granted,
            consent.RecordedAt,
            consent.Source.ToString().ToLowerInvariant(),
            consent.RecordedByUserId,
            consent.Note);
}

/// <summary>A data-subject request as the API returns it.</summary>
public sealed record DataRequestDto(
    string Id,
    uint Version,
    string CustomerId,
    string Type,
    string Status,
    DateTimeOffset RaisedAt,
    string? Detail,
    DateTimeOffset? ClosedAt,
    string? ClosedByUserId,
    string? Resolution,
    int OpenDays)
{
    public static DataRequestDto From(DataSubjectRequest request, DateTimeOffset now) =>
        new(
            request.Id,
            request.Version,
            request.CustomerId,
            request.Type.ToString().ToLowerInvariant(),
            request.Status.ToString().ToLowerInvariant(),
            request.RaisedAt,
            request.Detail,
            request.ClosedAt,
            request.ClosedByUserId,
            request.Resolution,
            (int)request.AgeAt(now).TotalDays);
}
