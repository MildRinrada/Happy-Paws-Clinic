namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// The request body or query failed validation before any work was attempted.
/// <para>
/// The envelope carries a single <c>field</c>, so the first failure is promoted there and the full
/// per-field map goes into <c>details.errors</c>. Messages come from FluentValidation validators
/// that live next to the use case they guard.
/// </para>
/// </summary>
public sealed class ValidationFailedException(IReadOnlyDictionary<string, string[]> errors)
    : AppException(ErrorCodes.ValidationFailed, "ข้อมูลที่ส่งมาไม่ถูกต้อง", HttpStatus.UnprocessableEntity)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    /// <summary>First offending field, promoted to <c>error.field</c>.</summary>
    public string? FirstField => Errors.Keys.FirstOrDefault();
}
