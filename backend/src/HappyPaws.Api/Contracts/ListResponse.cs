using System.Text.Json.Serialization;

namespace HappyPaws.Api.Contracts;

/// <summary>
/// The envelope for a list short enough to return whole — branches, roles, the scope catalogue,
/// service categories:
/// <code>{ "data": [ ... ] }</code>
/// <para>
/// The same <c>data</c> key the paginated envelope uses, without a <c>page</c>. A client reads a
/// collection the same way wherever it came from, and a list that outgrows this can gain a
/// <c>page</c> later without its existing readers noticing.
/// </para>
/// </summary>
public sealed record ListResponse<T>([property: JsonPropertyName("data")] IReadOnlyList<T> Data);

/// <summary>Spares every call site the type argument.</summary>
public static class ListResponse
{
    public static ListResponse<T> Of<T>(IReadOnlyList<T> data) => new(data);
}
