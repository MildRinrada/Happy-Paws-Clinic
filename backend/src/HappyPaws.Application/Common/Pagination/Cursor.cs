using System.Buffers.Text;
using System.Text;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Common.Pagination;

/// <summary>
/// Encodes and decodes the opaque <c>?cursor=</c> token.
/// <para>
/// A cursor is the sort key of the last row returned — a timestamp plus the row id, which
/// together are unique and stable. Offset paging would skip or repeat rows in clinic lists that
/// are appended to while a client pages through them (the queue, today's appointments, the audit
/// log), which is exactly why the contract specifies cursors.
/// </para>
/// <para>
/// The value is base64url of <c>sortKey|id</c>. It is opaque by convention, not secret: never put
/// anything in it that the caller is not already allowed to see, and always re-apply the caller's
/// authorization filters to the page it selects.
/// </para>
/// </summary>
public static class Cursor
{
    private const char Separator = '|';

    public static string Encode(DateTimeOffset sortKey, string id) =>
        Encode($"{sortKey.UtcDateTime:O}{Separator}{id}");

    /// <summary>Decodes a cursor, or throws <c>422 validation_failed</c> on a malformed one.</summary>
    public static (DateTimeOffset SortKey, string Id) Decode(string cursor)
    {
        var decoded = DecodeToString(cursor);
        var separator = decoded.IndexOf(Separator, StringComparison.Ordinal);

        if (separator > 0
            && DateTimeOffset.TryParse(decoded[..separator], out var sortKey)
            && separator + 1 < decoded.Length)
        {
            return (sortKey, decoded[(separator + 1)..]);
        }

        throw Malformed();
    }

    private static string Encode(string raw) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string DecodeToString(string cursor)
    {
        var padded = cursor.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - (padded.Length % 4)) % 4);

        Span<byte> buffer = new byte[padded.Length];
        if (!Convert.TryFromBase64String(padded, buffer, out var written))
        {
            throw Malformed();
        }

        return Encoding.UTF8.GetString(buffer[..written]);
    }

    private static ValidationFailedException Malformed() =>
        new(new Dictionary<string, string[]> { ["cursor"] = ["cursor ไม่ถูกต้อง"] });
}
