using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.UnitTests.Application.Pagination;

public class CursorTests
{
    [Fact]
    public void Round_trips_sort_key_and_id()
    {
        var sortKey = new DateTimeOffset(2026, 8, 22, 10, 30, 0, TimeSpan.FromHours(7));

        var (decodedKey, decodedId) = Cursor.Decode(Cursor.Encode(sortKey, "apt_24810"));

        Assert.Equal(sortKey.ToUniversalTime(), decodedKey.ToUniversalTime());
        Assert.Equal("apt_24810", decodedId);
    }

    [Fact]
    public void Encoded_cursor_is_url_safe()
    {
        // Cursors travel in a query string. '+' and '/' from plain base64 would be mangled by
        // encoders and proxies, silently breaking paging for some rows and not others.
        var cursor = Cursor.Encode(DateTimeOffset.UtcNow, "pet_luna");

        Assert.DoesNotContain('+', cursor);
        Assert.DoesNotContain('/', cursor);
        Assert.DoesNotContain('=', cursor);
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("")]
    [InlineData("!!!!")]
    public void Malformed_cursor_fails_validation_rather_than_throwing_something_unhandled(string cursor)
    {
        // A hostile or stale cursor must produce a 422 the client can act on, never a 500.
        Assert.Throws<ValidationFailedException>(() => Cursor.Decode(cursor));
    }

    [Fact]
    public void Page_request_clamps_a_hostile_limit()
    {
        // ?limit=100000 must not become a full table scan across a clinic's medical history.
        Assert.Equal(CursorPaging.MaxLimit, new CursorPageRequest(Limit: 100_000).SafeLimit);
        Assert.Equal(1, new CursorPageRequest(Limit: -5).SafeLimit);
        Assert.Equal(CursorPaging.DefaultLimit, new CursorPageRequest().SafeLimit);
    }
}
