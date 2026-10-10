using System.Data.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Reviews;
using HappyPaws.Infrastructure.Common;
using HappyPaws.Infrastructure.Persistence.Repositories.Analytics;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Reviews;

/// <summary>
/// Visits for the review pages, over <see cref="ReviewVisitSql"/> (<c>docs/contract-extensions.md § 36</c>).
/// Read-only; it never loads an aggregate.
/// </summary>
internal sealed class ReviewVisitReader(HappyPawsDbContext db, IClock clock, IOptions<ClinicOptions> clinic) : IReviewVisitReader
{
    public async Task<IReadOnlyList<ReviewableVisit>> ForCustomerAsync(
        string customerId,
        DateOnly firstDay,
        DateOnly lastDay,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = ReviewVisitSql.DaySpan(firstDay, lastDay, clock.ClinicTimeZone);

        return await AnalyticsSql.QueryAsync(
            db,
            ReviewVisitSql.Visits,
            ReviewVisitSql.Parameters(clinic.Value.TimeZone, from, to, customerId),
            Visit,
            cancellationToken);
    }

    public async Task<ReviewableVisit?> FindAsync(string visitId, CancellationToken cancellationToken = default)
    {
        var owners = await AnalyticsSql.QueryAsync(
            db,
            ReviewVisitSql.Owner,
            [new NpgsqlParameter("visit_id", NpgsqlDbType.Text) { Value = visitId }],
            reader => (CustomerId: reader.GetString(0), BranchId: reader.GetString(1), At: reader.GetFieldValue<DateTimeOffset>(2)),
            cancellationToken);

        if (owners.Count == 0)
        {
            return null;
        }

        var owner = owners[0];
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(owner.At, clock.ClinicTimeZone).DateTime);
        var visits = await ForCustomerAsync(owner.CustomerId, day, day, cancellationToken);

        return visits.FirstOrDefault(visit => visit.BranchId == owner.BranchId);
    }

    private static ReviewableVisit Visit(DbDataReader reader) => new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetFieldValue<DateOnly>(4),
        reader.GetString(5),
        reader.GetString(6),
        reader.GetString(7),
        reader.GetBoolean(8),
        reader.NullableString(9),
        reader.GetBoolean(10));
}
