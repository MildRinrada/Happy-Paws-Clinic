using System.Security.Cryptography;
using System.Text;
using HappyPaws.Api.Errors;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Api.Idempotency;

/// <summary>
/// Makes retries of money-, appointment- and queue-creating POSTs safe.
/// <para>
/// A flaky mobile connection, a double-tapped Confirm button and a gateway timeout the client
/// retries all look identical to the server. Without this, each one books the slot twice or
/// charges the card twice.
/// </para>
/// <para>
/// Only endpoints marked <see cref="IdempotentAttribute"/> are affected. A marked endpoint without
/// the header is refused rather than silently unprotected — an optional guard is only as good as
/// the least careful client.
/// </para>
/// </summary>
public sealed class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    /// <summary>Responses larger than this are stored but logged: a creation response this big is a design smell.</summary>
    private const int LargeResponseWarningBytes = 256 * 1024;

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore store, ICurrentUser currentUser)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IdempotentAttribute>() is null)
        {
            await next(context);
            return;
        }

        var headerValue = context.Request.Headers[RequestContext.IdempotencyKeyHeader].ToString();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            await ApiErrorWriter.WriteAsync(
                context,
                HttpStatus.BadRequest,
                ErrorCodes.IdempotencyKeyRequired,
                "ต้องส่ง Idempotency-Key สำหรับคำขอนี้",
                messageEn: "This endpoint requires an Idempotency-Key header.",
                field: RequestContext.IdempotencyKeyHeader);
            return;
        }

        var key = new IdempotencyKey(
            headerValue.Trim(),
            $"{context.Request.Method} {context.GetEndpoint()?.DisplayName ?? context.Request.Path}",
            currentUser.UserId ?? "anonymous");

        var requestHash = await HashRequestBodyAsync(context.Request);
        var existing = await store.TryClaimAsync(key, requestHash, context.RequestAborted);

        if (existing is not null)
        {
            await ReplayAsync(context, existing, key);
            return;
        }

        await RunAndRecordAsync(context, store, key);
    }

    private async Task ReplayAsync(HttpContext context, IdempotentOutcome outcome, IdempotencyKey key)
    {
        if (!outcome.RequestHashMatches)
        {
            // Same key, different body. This is a client bug, not a retry — replaying the earlier
            // answer would hide it and could confirm an operation the caller never asked for.
            logger.LogWarning("Idempotency key {Key} reused with a different request body", key.Value);

            await ApiErrorWriter.WriteAsync(
                context,
                HttpStatus.Conflict,
                ErrorCodes.IdempotencyKeyReused,
                "Idempotency-Key นี้ถูกใช้กับคำขอที่มีเนื้อหาต่างกันแล้ว",
                messageEn: "This Idempotency-Key was already used with a different request body.",
                field: RequestContext.IdempotencyKeyHeader);
            return;
        }

        if (outcome.IsInFlight)
        {
            await ApiErrorWriter.WriteAsync(
                context,
                HttpStatus.Conflict,
                ErrorCodes.RequestInFlight,
                "คำขอเดิมกำลังดำเนินการอยู่ กรุณารอสักครู่",
                messageEn: "An identical request is still being processed. Retry shortly.");
            return;
        }

        context.Response.StatusCode = outcome.StatusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers["Idempotent-Replay"] = "true";

        if (outcome.ResponseBody is { Length: > 0 } body)
        {
            await context.Response.WriteAsync(body);
        }
    }

    private async Task RunAndRecordAsync(HttpContext context, IIdempotencyStore store, IdempotencyKey key)
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);

            var status = context.Response.StatusCode;

            // 5xx is not a settled outcome — the operation may not have happened, so the client
            // must be free to retry with the same key. Anything else is deterministic and is
            // stored so the retry replays it.
            if (status >= 500)
            {
                await store.ReleaseAsync(key, CancellationToken.None);
            }
            else
            {
                if (buffer.Length > LargeResponseWarningBytes)
                {
                    logger.LogWarning(
                        "Idempotent response for {Endpoint} is {Bytes} bytes; consider returning a reference instead",
                        key.Endpoint,
                        buffer.Length);
                }

                var responseBody = Encoding.UTF8.GetString(buffer.ToArray());
                await store.CompleteAsync(key, status, responseBody, CancellationToken.None);
            }
        }
        catch
        {
            // The claim must not outlive a faulted request, or the client's legitimate retry is
            // refused as "in flight" until the record expires 24 hours later.
            await store.ReleaseAsync(key, CancellationToken.None);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
            buffer.Position = 0;

            if (!context.Response.HasStarted || buffer.Length > 0)
            {
                await buffer.CopyToAsync(originalBody, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Hashes the request body so that reusing a key with different content can be detected.
    /// Buffering is enabled first, because model binding needs to read the stream afterwards.
    /// </summary>
    private static async Task<string> HashRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(request.Body);

        request.Body.Position = 0;
        return Convert.ToHexStringLower(hash);
    }
}
