namespace HappyPaws.Domain.Common;

/// <summary>
/// An illegal state transition — closing a case that was never called, cancelling an appointment
/// that already completed, paying an invoice that was voided.
/// <para>
/// Surfaces as <c>409 state_conflict</c>. The current and attempted states go into
/// <c>error.details</c> so the client can tell the user what actually happened rather than
/// showing a generic conflict.
/// </para>
/// </summary>
public sealed class StateConflictException(string entity, string currentState, string attemptedTransition)
    : DomainException("state_conflict", $"ไม่สามารถเปลี่ยนสถานะ {entity} จาก {currentState} ด้วย {attemptedTransition}")
{
    public string Entity { get; } = entity;

    public string CurrentState { get; } = currentState;

    public string AttemptedTransition { get; } = attemptedTransition;
}
