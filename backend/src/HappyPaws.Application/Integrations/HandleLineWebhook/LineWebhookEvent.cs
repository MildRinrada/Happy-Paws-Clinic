using System.Text.Json;

namespace HappyPaws.Application.Integrations.HandleLineWebhook;

/// <summary>
/// The parts of a LINE webhook event this system reads. Everything else LINE sends (timestamps,
/// delivery context, the content of a picture or a file) is ignored rather than stored.
/// </summary>
internal sealed record LineWebhookEvent(
    string Type,
    string? UserId,
    string? ReplyToken,
    string? Mode,
    string? PostbackData,
    LineIncomingMessage? Message = null);

/// <summary>
/// The message of a <c>message</c> event: LINE's id for it, its type (<c>text</c>, <c>image</c>,
/// <c>sticker</c>...), and its words: the text, a file's name, or a location's title and address.
/// </summary>
internal sealed record LineIncomingMessage(string? Id, string Type, string? Text);

internal static class LineWebhookPayload
{
    /// <summary>The events in a webhook body, or null when the body is not the shape LINE sends.</summary>
    public static IReadOnlyList<LineWebhookEvent>? Parse(ReadOnlyMemory<byte> body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return
            [
                .. events.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.Object)
                    .Select(item => new LineWebhookEvent(
                        Text(item, "type") ?? string.Empty,
                        item.TryGetProperty("source", out var source) && Text(source, "type") == "user"
                            ? Text(source, "userId")
                            : null,
                        Text(item, "replyToken"),
                        Text(item, "mode"),
                        item.TryGetProperty("postback", out var postback) ? Text(postback, "data") : null,
                        item.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object
                            ? new LineIncomingMessage(Text(message, "id"), Text(message, "type") ?? string.Empty, Words(message))
                            : null)),
            ];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Words(JsonElement message) => Text(message, "type") switch
    {
        "text" => Text(message, "text"),
        "file" => Text(message, "fileName"),
        "location" => string.Join(" ", new[] { Text(message, "title"), Text(message, "address") }.Where(part => !string.IsNullOrWhiteSpace(part))),
        _ => null,
    };

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
