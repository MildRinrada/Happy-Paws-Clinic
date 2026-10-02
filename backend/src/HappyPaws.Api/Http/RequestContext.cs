using HappyPaws.Application.Common.Abstractions;
using Microsoft.Net.Http.Headers;

namespace HappyPaws.Api.Http;

/// <summary>
/// Per-request metadata read off the incoming <c>HttpContext</c>.
/// </summary>
public sealed class RequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    public const string RequestIdHeader = "X-Request-Id";
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    private HttpContext? Context => accessor.HttpContext;

    public string RequestId => Context?.TraceIdentifier ?? "req_unknown";

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => Context?.Request.Headers.UserAgent.ToString();

    public string? IdempotencyKey
    {
        get
        {
            var value = Context?.Request.Headers[IdempotencyKeyHeader].ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    public uint? IfMatchVersion => IfMatch.Parse(Context?.Request.Headers[HeaderNames.IfMatch].ToString());
}
