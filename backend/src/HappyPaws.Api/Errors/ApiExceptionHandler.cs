using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Api.Errors;

/// <summary>
/// Translates every exception into the shared error envelope.
/// <para>
/// Two rules govern this file. First, an expected failure keeps its meaning: a
/// <see cref="DomainException"/> or <see cref="AppException"/> carries a contract code and status
/// and is reported as such. Second, an <b>unexpected</b> failure never leaks — no message, no
/// stack, no SQL. The client gets <c>internal_error</c> and a request id; the detail goes to the
/// log where it belongs. In a system holding medical records, an exception message echoed to the
/// caller is a data disclosure.
/// </para>
/// </summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationFailedException validation:
                Log(logger, context, validation, LogLevel.Information);
                await ApiErrorWriter.WriteAsync(
                    context,
                    validation.StatusCode,
                    validation.Code,
                    validation.Message,
                    messageEn: "Request validation failed.",
                    field: validation.FirstField,
                    details: new Dictionary<string, object> { ["errors"] = validation.Errors });
                return true;

            case InsufficientScopeException scope:
                Log(logger, context, scope, LogLevel.Warning);
                await ApiErrorWriter.WriteAsync(
                    context,
                    scope.StatusCode,
                    scope.Code,
                    scope.Message,
                    messageEn: "The token is missing one or more required scopes.",
                    details: new Dictionary<string, object> { ["missing_scopes"] = scope.MissingScopes });
                return true;

            case VersionConflictException version:
                Log(logger, context, version, LogLevel.Information);
                await ApiErrorWriter.WriteAsync(
                    context,
                    version.StatusCode,
                    version.Code,
                    version.Message,
                    messageEn: "The resource was modified by someone else. Re-read and retry.",
                    details: new Dictionary<string, object>
                    {
                        ["expected_version"] = version.ExpectedVersion,
                        ["current_version"] = version.CurrentVersion,
                    });
                return true;

            case AppException app:
                // ForbiddenException and NotFoundException land here. Their Reason / ResourceId
                // are logged, never returned — that is what keeps a 404 from confirming that an
                // id exists.
                Log(logger, context, app, LogLevel.Information);
                await ApiErrorWriter.WriteAsync(
                    context, app.StatusCode, app.Code, app.Message, app.MessageEn, app.Field, app.Details);
                return true;

            case RecordLockedException locked:
                Log(logger, context, locked, LogLevel.Information);
                await ApiErrorWriter.WriteAsync(
                    context,
                    HttpStatus.Locked,
                    locked.Code,
                    locked.Message,
                    messageEn: "The medical record is signed. Submit an amendment instead.");
                return true;

            case StateConflictException state:
                Log(logger, context, state, LogLevel.Information);
                await ApiErrorWriter.WriteAsync(
                    context,
                    HttpStatus.Conflict,
                    state.Code,
                    state.Message,
                    messageEn: "Illegal state transition.",
                    details: new Dictionary<string, object>
                    {
                        ["current_state"] = state.CurrentState,
                        ["attempted"] = state.AttemptedTransition,
                    });
                return true;

            case DomainException domain:
                Log(logger, context, domain, LogLevel.Information);
                await ApiErrorWriter.WriteAsync(context, HttpStatus.Conflict, domain.Code, domain.Message);
                return true;

            case DbUpdateConcurrencyException concurrency:
                // EF's own optimistic-concurrency failure, from the xmin token. Reaching here
                // means a use case mutated without an explicit ConcurrencyGuard check; the answer
                // is still 409, but it is worth a warning so the gap gets closed.
                Log(logger, context, concurrency, LogLevel.Warning);
                await ApiErrorWriter.WriteAsync(
                    context,
                    HttpStatus.Conflict,
                    ErrorCodes.VersionConflict,
                    "ข้อมูลถูกแก้ไขโดยผู้อื่นแล้ว กรุณาโหลดใหม่",
                    messageEn: "The resource was modified concurrently.");
                return true;

            case OperationCanceledException when context.RequestAborted.IsCancellationRequested:
                // The client hung up. Not an error, and not worth a log entry at warning level.
                return true;

            default:
                logger.LogError(
                    exception,
                    "Unhandled exception on {Method} {Path} (request {RequestId})",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);

                await ApiErrorWriter.WriteAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    ErrorCodes.InternalError,
                    "เกิดข้อผิดพลาดภายในระบบ",
                    messageEn: "An unexpected error occurred.");
                return true;
        }
    }

    private static void Log(ILogger logger, HttpContext context, Exception exception, LogLevel level) =>
        logger.Log(
            level,
            "{ExceptionType} on {Method} {Path} (request {RequestId}): {Detail}",
            exception.GetType().Name,
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier,
            exception.Message);
}
