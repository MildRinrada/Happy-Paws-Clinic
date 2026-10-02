using System.Text.Json.Serialization;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.Api.Contracts;

/// <summary>
/// The list envelope every paginated endpoint returns:
/// <code>{ "data": [ ... ], "page": { "next_cursor": "..." } }</code>
/// </summary>
public sealed record CursorPageResponse<T>(
    [property: JsonPropertyName("data")] IReadOnlyList<T> Data,
    [property: JsonPropertyName("page")] PageInfo Page)
{
    public static CursorPageResponse<T> From(CursorPage<T> page) =>
        new(page.Data, new PageInfo(page.NextCursor));
}

/// <param name="NextCursor">Pass back as <c>?cursor=</c>. Null on the last page.</param>
public sealed record PageInfo([property: JsonPropertyName("next_cursor")] string? NextCursor);
