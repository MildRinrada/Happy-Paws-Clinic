namespace HappyPaws.Infrastructure.Persistence.Idempotency;

/// <summary>
/// One claimed <c>Idempotency-Key</c> and, once the request finishes, the response to replay.
/// <para>
/// Deliberately not a domain entity. It carries no clinic meaning — it is a technical record that
/// makes retries safe — so it lives in Infrastructure and never appears in the Domain model.
/// </para>
/// </summary>
public sealed class IdempotencyRecord
{
    /// <summary>The client-supplied key.</summary>
    public required string Key { get; init; }

    /// <summary>Method + route template. The same key on a different endpoint is a different operation.</summary>
    public required string Endpoint { get; init; }

    /// <summary>Account the request was made for, so keys cannot collide between callers.</summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Hash of the request body. Reusing a key with different content is a client bug, not a
    /// retry, and must be reported rather than answered with the earlier response.
    /// </summary>
    public required string RequestHash { get; init; }

    /// <summary>Null until the original request finishes.</summary>
    public int? StatusCode { get; set; }

    /// <summary>Null until the original request finishes.</summary>
    public string? ResponseBody { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Rows are swept after this instant; the contract's retry window is 24 hours.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    public bool IsInFlight => CompletedAt is null;
}
