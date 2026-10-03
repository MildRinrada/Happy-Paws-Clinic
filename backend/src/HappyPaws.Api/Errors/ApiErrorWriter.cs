using System.Text.Json;
using HappyPaws.Api.Contracts;

namespace HappyPaws.Api.Errors;

/// <summary>
/// The single place that writes the error envelope onto a response.
/// <para>
/// Both the exception handler and the authorization result handler go through it, so a 403 from
/// the middleware pipeline is byte-for-byte the same shape as a 422 from a use case. Clients
/// parse one structure, always.
/// </para>
/// </summary>
public static class ApiErrorWriter
{
    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        string? messageEn = null,
        string? field = null,
        IReadOnlyDictionary<string, object>? details = null)
    {
        if (context.Response.HasStarted)
        {
            // Nothing useful left to do: the status line is already on the wire. Logged by the
            // caller; overwriting here would corrupt the response body.
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = new ApiErrorResponse(new ApiError
        {
            Code = code,
            Message = message,
            MessageEn = messageEn,
            Field = field,
            RequestId = context.TraceIdentifier,
            Details = details,
        });

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
    }

    /// <summary>
    /// The same wire format as every success response (see <c>Program.cs</c>). Objects carried in
    /// <c>details</c>, such as the customers listed by <c>duplicate_customer</c>, are records like
    /// any other response and must read <c>display_name</c>, not <c>displayName</c>. Dictionary keys
    /// are left as written: they are already the API's own names (<c>existing</c>, a field name).
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,

        // Thai messages must survive unescaped so a client can display them without decoding.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
