using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Api.Middleware;

/// <summary>
/// Assigns the request id and opens the log scope every later log line inherits.
/// <para>
/// Runs first in the pipeline, so even a failure inside authentication carries the same
/// <c>request_id</c> the client sees in the error envelope. A support call that starts with
/// "it said req_01J9XN4" should end at one log query.
/// </para>
/// <para>
/// The scope carries identifiers only — never a pet name, an owner's phone number or anything from
/// a medical record. Logs are replicated, shipped and retained on different rules than the
/// database, and clinical data must not spread through them.
/// </para>
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        // Honour a caller-supplied id so a trace spans the frontend and the API, but generate our
        // own when absent. Either way it goes back on the response so the client can log it too.
        var incoming = context.Request.Headers[RequestContext.RequestIdHeader].ToString();
        context.TraceIdentifier = string.IsNullOrWhiteSpace(incoming)
            ? $"req_{Guid.NewGuid():N}"[..16]
            : Sanitise(incoming);

        // Registered as an OnStarting callback rather than written now: an error response is
        // built with Response.Clear(), which discards headers set earlier in the pipeline. This
        // way the id is stamped as the response begins, so it survives on the 4xx and 5xx paths
        // where a caller most needs it.
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            ctx.Response.Headers[RequestContext.RequestIdHeader] = ctx.TraceIdentifier;
            return Task.CompletedTask;
        }, context);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestId"] = context.TraceIdentifier,
            ["UserId"] = currentUser.UserId,
            ["StaffId"] = currentUser.StaffId,
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path.Value,
        });

        await next(context);
    }

    /// <summary>
    /// A client-supplied id is echoed into logs and response headers, so it is bounded and
    /// stripped of anything that could forge a log line or split a header.
    /// </summary>
    private static string Sanitise(string value)
    {
        var trimmed = value.Length > 64 ? value[..64] : value;
        return new string([.. trimmed.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')]);
    }
}
