using System.Text.Json;
using System.Text.Json.Serialization;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.Api.Http;

/// <summary>
/// Writes every <see cref="CursorPage{T}"/> in the contract's list envelope:
/// <code>{ "data": [ ... ], "page": { "next_cursor": "..." } }</code>
/// <para>
/// A converter rather than a wrapper each controller has to remember. A page returned straight
/// from a handler would otherwise serialise by its own shape — <c>next_cursor</c> at the top level,
/// no <c>page</c> object — which is a different contract from the one the reference states
/// ("คืน data[] และ page.next_cursor"), and a client written against one cannot page the other.
/// Registered once, it makes the envelope a property of the type instead of a habit.
/// </para>
/// <para>
/// <c>next_cursor</c> is omitted on the last page, like every other null in this API.
/// </para>
/// </summary>
public sealed class CursorPageJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(CursorPage<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(Converter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    private sealed class Converter<T> : JsonConverter<CursorPage<T>>
    {
        public override CursorPage<T> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            var data = root.TryGetProperty("data", out var items)
                ? items.Deserialize<List<T>>(options) ?? []
                : [];

            var nextCursor = root.TryGetProperty("page", out var page)
                && page.TryGetProperty("next_cursor", out var cursor)
                    ? cursor.GetString()
                    : null;

            return new CursorPage<T>(data, nextCursor);
        }

        public override void Write(Utf8JsonWriter writer, CursorPage<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("data");
            JsonSerializer.Serialize(writer, value.Data, options);

            writer.WriteStartObject("page");

            if (value.NextCursor is not null)
            {
                writer.WriteString("next_cursor", value.NextCursor);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }
}
