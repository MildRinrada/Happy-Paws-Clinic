namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// Per-request metadata that is not about identity.
/// <para>
/// <see cref="RequestId"/> is the value echoed in every error envelope's <c>request_id</c> and
/// attached to every log line for the request, so a customer quoting the id from an error screen
/// leads straight to the trace.
/// </para>
/// </summary>
public interface IRequestContext
{
    /// <summary>Correlation id, e.g. <c>req_01J9XN4</c>.</summary>
    string RequestId { get; }

    /// <summary>Caller IP, recorded in the audit log ("จาก IP ใด").</summary>
    string? IpAddress { get; }

    /// <summary>User agent, recorded alongside sessions and audit entries.</summary>
    string? UserAgent { get; }

    /// <summary><c>Idempotency-Key</c> when the caller sent one.</summary>
    string? IdempotencyKey { get; }

    /// <summary>Parsed <c>If-Match</c> version when the caller sent one.</summary>
    uint? IfMatchVersion { get; }
}
