namespace HappyPaws.Domain.Common;

/// <summary>
/// Raised when a domain invariant or state transition is violated
/// (for example closing a consultation whose queue ticket was never called).
/// The API layer translates this into the shared error envelope — typically
/// <c>409 state_conflict</c> — without leaking internal detail.
/// </summary>
public class DomainException(string code, string message) : Exception(message)
{
    /// <summary>Stable machine-readable code carried through to <c>error.code</c>.</summary>
    public string Code { get; } = code;
}
