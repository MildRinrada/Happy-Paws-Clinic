namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// A <c>409</c> with its own code: the request is well formed, but it clashes with something
/// already recorded, e.g. a shift on top of another. <see cref="AppException.Details"/> says what
/// it clashed with, so a client can show the person rather than make them guess.
/// </summary>
public sealed class ConflictException(string code, string messageTh)
    : AppException(code, messageTh, HttpStatus.Conflict);
