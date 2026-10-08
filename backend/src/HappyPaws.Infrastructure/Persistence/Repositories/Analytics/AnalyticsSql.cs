using System.Data;
using System.Data.Common;
using HappyPaws.Application.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Analytics;

/// <summary>
/// Runs a report's hand-written SQL on the request's own connection and maps each row by hand.
/// <para>
/// Plain ADO.NET rather than <c>SqlQueryRaw&lt;T&gt;</c>: the rows are positional tuples with
/// casts in them (<c>COUNT(*)::int</c>, <c>AT TIME ZONE</c>), and reading them by ordinal keeps the
/// mapping next to the SELECT it belongs to instead of in a naming convention.
/// </para>
/// <para>
/// Every value is a bound parameter. The SQL text is a compile-time constant at every call site;
/// nothing from a request is ever spliced into it.
/// </para>
/// </summary>
internal static class AnalyticsSql
{
    public static async Task<List<T>> QueryAsync<T>(
        HappyPawsDbContext db,
        string sql,
        IEnumerable<NpgsqlParameter> parameters,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<T>();

            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(map(reader));
            }

            return rows;
        }
        finally
        {
            if (openedHere)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    /// <summary>
    /// The parameters every report shares: the clinic's zone, the window as instants and as days,
    /// and the branch filter.
    /// <para>
    /// Instants go over as UTC because Npgsql writes <c>timestamptz</c> only from offset zero.
    /// The branch filter is a flag plus an array, so the SQL stays one constant string:
    /// <c>(@all_branches OR x.branch_id = ANY(@branches))</c>.
    /// </para>
    /// </summary>
    public static List<NpgsqlParameter> WindowParameters(ReportWindow window, string timeZone) =>
    [
        new("tz", NpgsqlDbType.Text) { Value = timeZone },
        new("from", NpgsqlDbType.TimestampTz) { Value = window.From.ToUniversalTime() },
        new("to", NpgsqlDbType.TimestampTz) { Value = window.To.ToUniversalTime() },
        new("first", NpgsqlDbType.Date) { Value = window.First },
        new("last", NpgsqlDbType.Date) { Value = window.Last },
        new("all_branches", NpgsqlDbType.Boolean) { Value = window.BranchIds is null },
        new("branches", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = window.BranchIds?.ToArray() ?? [] },
    ];

    public static NpgsqlParameter Text(string name, string? value) =>
        new(name, NpgsqlDbType.Text) { Value = (object?)value ?? DBNull.Value };

    public static NpgsqlParameter Date(string name, DateOnly value) =>
        new(name, NpgsqlDbType.Date) { Value = value };

    public static string? NullableString(this DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    public static T ParseEnum<T>(this DbDataReader reader, int ordinal)
        where T : struct, Enum =>
        Enum.Parse<T>(reader.GetString(ordinal));
}
