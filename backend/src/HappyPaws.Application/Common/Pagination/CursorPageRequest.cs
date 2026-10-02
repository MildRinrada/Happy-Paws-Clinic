namespace HappyPaws.Application.Common.Pagination;

/// <summary>
/// The contract's paging inputs: <c>?limit=25&amp;cursor=</c>.
/// </summary>
public sealed record CursorPageRequest(int Limit = CursorPaging.DefaultLimit, string? Cursor = null)
{
    /// <summary>Limit clamped into the accepted range, so a hostile <c>?limit=100000</c> cannot force a table scan.</summary>
    public int SafeLimit => Math.Clamp(Limit, 1, CursorPaging.MaxLimit);
}

/// <summary>Paging bounds shared by every list endpoint.</summary>
public static class CursorPaging
{
    public const int DefaultLimit = 25;
    public const int MaxLimit = 100;
}
