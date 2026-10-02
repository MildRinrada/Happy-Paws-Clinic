using HappyPaws.Application.Common.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HappyPaws.Api.Errors;

/// <summary>
/// Routes model-binding failures into the same envelope as everything else.
/// <para>
/// Without this, two different things happen to a bad request depending on <b>where</b> it was
/// caught. A body that reaches a use case and fails its FluentValidation rules answers
/// <c>422 validation_failed</c> with the shared envelope; a body missing a required field never
/// gets that far — <c>[ApiController]</c>'s own filter short-circuits it into ASP.NET's
/// <c>ValidationProblemDetails</c> at <c>400</c>. Same class of mistake, two status codes and two
/// response shapes, and every client would have to parse both.
/// </para>
/// <para>
/// The fix is to throw rather than to write a response: <see cref="ApiExceptionHandler"/> already
/// knows how to render a <see cref="ValidationFailedException"/>, so this produces a byte-identical
/// body rather than a second implementation of the same envelope that could drift from it.
/// </para>
/// </summary>
public static class ModelStateErrors
{
    public static void UseSharedEnvelope(ApiBehaviorOptions options) =>
        options.InvalidModelStateResponseFactory = context => throw FromModelState(context.ModelState);

    private static ValidationFailedException FromModelState(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                // The binder names the body parameter itself for a whole-body failure — "$" or
                // "request". Neither is a field a client can point a user at, so it travels as an
                // empty key and the envelope reports the request rather than a phantom field.
                entry => FieldName(entry.Key),
                entry => entry.Value!.Errors.Select(Message).ToArray(),
                StringComparer.Ordinal);

        return new ValidationFailedException(errors);
    }

    private static string FieldName(string key) =>
        key is "$" or "request" ? string.Empty : key;

    private static string Message(ModelError error) =>
        // The binder's own text is English and mentions .NET type names. Anything it produced
        // without an exception is a stated rule and safe to pass through; the rest would leak
        // internals into a message a client may display.
        string.IsNullOrWhiteSpace(error.ErrorMessage) ? "ข้อมูลไม่ถูกต้อง" : error.ErrorMessage;
}
