namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// Base for failures that are part of the API contract rather than bugs.
/// <para>
/// Anything deriving from this is a <b>deliberate, expected</b> outcome: the caller asked for
/// something that is not allowed, not found, or no longer current. The API translates it straight
/// into the shared error envelope with the status code carried here. Anything that does *not*
/// derive from this is an unexpected fault and becomes a <c>500</c> with no internal detail
/// leaked.
/// </para>
/// </summary>
public abstract class AppException(string code, string message, int statusCode) : Exception(message)
{
    /// <summary>Machine-readable <c>error.code</c>. Clients switch on this, never on the message.</summary>
    public string Code { get; } = code;

    /// <summary>HTTP status this failure maps to.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>English message for logs and non-Thai clients. Falls back to <see cref="Exception.Message"/>.</summary>
    public string? MessageEn { get; init; }

    /// <summary>Offending request field, when the failure is input-related.</summary>
    public string? Field { get; init; }

    /// <summary>Code-specific payload — missing scopes, suggested slots, the conflicting version.</summary>
    public IReadOnlyDictionary<string, object>? Details { get; init; }
}
