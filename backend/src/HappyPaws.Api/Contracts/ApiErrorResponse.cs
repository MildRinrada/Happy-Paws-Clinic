using System.Text.Json.Serialization;

namespace HappyPaws.Api.Contracts;

/// <summary>
/// The one error envelope every endpoint returns, exactly as specified in the API reference:
/// <code>
/// {
///   "error": {
///     "code": "slot_unavailable",
///     "message": "ช่วงเวลานี้ถูกจองแล้ว",
///     "message_en": "Slot already booked",
///     "field": "starts_at",
///     "request_id": "req_01J9XN4",
///     "details": { "suggested": ["11:00","15:30","17:00"] }
///   }
/// }
/// </code>
/// Exception handling middleware is the only place that constructs this — controllers do not
/// hand-roll error bodies, and no internal exception text ever reaches <c>message</c>.
/// </summary>
public sealed record ApiErrorResponse([property: JsonPropertyName("error")] ApiError Error);

/// <summary>The error object carried inside <see cref="ApiErrorResponse"/>.</summary>
public sealed record ApiError
{
    /// <summary>Stable machine-readable code, e.g. <c>insufficient_scope</c>.</summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>Human-readable Thai message shown to the end user.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>English equivalent for non-Thai clients and logs.</summary>
    [JsonPropertyName("message_en")]
    public string? MessageEn { get; init; }

    /// <summary>Offending request field when the failure is input-related.</summary>
    [JsonPropertyName("field")]
    public string? Field { get; init; }

    /// <summary>Correlation id, echoed in logs so support can trace one call.</summary>
    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    /// <summary>Code-specific payload — missing scopes, suggested slots, conflicting version.</summary>
    [JsonPropertyName("details")]
    public IReadOnlyDictionary<string, object>? Details { get; init; }
}
