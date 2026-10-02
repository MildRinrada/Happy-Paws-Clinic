using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HappyPaws.Infrastructure.Persistence;

/// <summary>
/// Allocates the next number in a dash-separated document series — <c>INV-202608-0042</c>,
/// <c>RCP-202608-0042</c>, <c>HN-2026-0042</c>.
/// <para>
/// Shared by the three generators that need it, because getting it wrong is silent and the two ways
/// to get it wrong are not obvious.
/// </para>
/// <para>
/// <b>The lock.</b> <c>pg_advisory_xact_lock</c>, keyed on the series, serialises allocation inside
/// the caller's transaction. It is released when that transaction ends — which is also what makes a
/// rolled-back document give its number back instead of leaving a gap. A gap in an invoice or
/// hospital-number sequence is something the clinic has to explain to somebody.
/// </para>
/// <para>
/// <b>The parse.</b> The suffix is cast to an integer rather than compared as text. Zero-padded
/// strings sort numerically only up to the padding width — <c>'10000'</c> sorts before
/// <c>'9999'</c> — so a clinic that crossed that line would quietly start reissuing numbers from
/// the beginning of the series. Max rather than count, so a row removed by a data fix never causes
/// a number to be printed twice.
/// </para>
/// </summary>
internal static class DocumentSequence
{
    /// <summary>
    /// The next integer for <paramref name="prefix"/> in <paramref name="table"/>, starting at 1.
    /// </summary>
    /// <param name="db">The context, whose ambient transaction the lock belongs to.</param>
    /// <param name="table">Table holding the series. Never caller-supplied.</param>
    /// <param name="column">Column holding the number. Never caller-supplied.</param>
    /// <param name="prefix">Everything before the running part, including its trailing dash.</param>
    /// <param name="segment">1-based index of the running part within the dash-separated number.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    public static async Task<int> NextAsync(
        HappyPawsDbContext db,
        string table,
        string column,
        string prefix,
        int segment,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})", [LockKey(prefix)], cancellationToken);

        // Identifiers and the split_part index are interpolated because PostgreSQL cannot bind
        // either — both are parsed, not bound. All three are compile-time constants at every call
        // site; there is no path from a request to here.
        var sql =
            $"""
             SELECT COALESCE(MAX(CAST(split_part({column}, '-', {segment}) AS integer)), 0) AS "Value"
             FROM {table}
             WHERE {column} LIKE @prefix
             """;

        var highest = await db.Database
            .SqlQueryRaw<int>(sql, new NpgsqlParameter("prefix", $"{prefix}%"))
            .FirstAsync(cancellationToken);

        return highest + 1;
    }

    /// <summary>
    /// A stable 64-bit key for one series. Advisory locks share a single global namespace of
    /// integers, so the hash is what keeps invoice numbering from blocking on hospital numbering.
    /// </summary>
    private static long LockKey(string prefix) =>
        BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes($"document:{prefix}")), 0);
}
