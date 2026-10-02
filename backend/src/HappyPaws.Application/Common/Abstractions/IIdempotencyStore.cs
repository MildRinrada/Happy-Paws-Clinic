namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// Persistence for <c>Idempotency-Key</c> replay protection.
/// <para>
/// The contract requires the header on every POST that creates money, an appointment or a queue
/// ticket. Without a store, a retried request — a flaky mobile connection, a double-tapped Confirm
/// button, a gateway timeout the client retries — books the slot twice or charges the card twice.
/// </para>
/// <para>
/// Records are keyed by <c>(key, endpoint, subject)</c> so one customer's key cannot collide with
/// another's, and hold a hash of the request body so that reusing a key with <i>different</i>
/// content is rejected rather than silently replaying the old answer.
/// </para>
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Claims the key for this request. Returns the previously stored outcome when the key has
    /// already been used, or null when this caller now owns it and should proceed.
    /// </summary>
    Task<IdempotentOutcome?> TryClaimAsync(IdempotencyKey key, string requestHash, CancellationToken cancellationToken = default);

    /// <summary>Stores the response so an identical retry can be replayed verbatim.</summary>
    Task CompleteAsync(IdempotencyKey key, int statusCode, string? responseBody, CancellationToken cancellationToken = default);

    /// <summary>Releases a claim whose request faulted, so the caller may legitimately retry.</summary>
    Task ReleaseAsync(IdempotencyKey key, CancellationToken cancellationToken = default);
}

/// <summary>Identifies one idempotent operation attempt.</summary>
/// <param name="Value">The client-supplied <c>Idempotency-Key</c> (a UUID).</param>
/// <param name="Endpoint">Method + route template, so the same key on a different endpoint is a different operation.</param>
/// <param name="Subject">The account the request was made for, so keys cannot collide across callers.</param>
public readonly record struct IdempotencyKey(string Value, string Endpoint, string Subject);

/// <summary>The stored result of an earlier attempt with the same key.</summary>
/// <param name="IsInFlight">True when the first attempt has not finished yet; the retry should back off.</param>
/// <param name="RequestHashMatches">False when the key was reused with a different body — a client bug worth reporting.</param>
/// <param name="StatusCode">Status of the completed original response.</param>
/// <param name="ResponseBody">Body of the completed original response, replayed verbatim.</param>
public sealed record IdempotentOutcome(bool IsInFlight, bool RequestHashMatches, int StatusCode, string? ResponseBody);
