namespace HappyPaws.Application.Common.Pagination;

/// <summary>
/// The contract's list envelope: <c>{ "data": [...], "page": { "next_cursor": "..." } }</c>.
/// Cursor-based, never offset-based — clinic lists (queue, appointments, audit logs) are
/// appended to while a client is paging through them.
/// </summary>
/// <typeparam name="T">The item type carried in <c>data</c>.</typeparam>
public sealed record CursorPage<T>(IReadOnlyList<T> Data, string? NextCursor)
{
    public static CursorPage<T> Empty { get; } = new([], null);
}
