using System.Globalization;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Common.Abstractions;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Billing;

/// <summary>
/// Allocates <c>INV-202608-0042</c> and <c>RCP-202608-0042</c>.
/// <para>
/// The month comes from the <b>clinic's</b> calendar, not UTC. A bill rung up at 1 a.m. on the
/// first of September in Bangkok is 6 p.m. on the thirty-first of August in UTC, and a document
/// numbered into the previous month is a document filed against the wrong tax period.
/// </para>
/// <para>
/// The locking and the suffix parse live in <see cref="DocumentSequence"/>, shared with hospital
/// numbers; the rationale for both is written there.
/// </para>
/// </summary>
internal sealed class DocumentNumberGenerator(HappyPawsDbContext db, IClock clock) : IDocumentNumberGenerator
{
    public Task<string> NextInvoiceNumberAsync(
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken = default) =>
        NextAsync("INV", "invoices", issuedAt, cancellationToken);

    public Task<string> NextReceiptNumberAsync(
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken = default) =>
        NextAsync("RCP", "receipts", issuedAt, cancellationToken);

    private async Task<string> NextAsync(
        string kind,
        string table,
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken)
    {
        var period = TimeZoneInfo.ConvertTime(issuedAt, clock.ClinicTimeZone)
            .ToString("yyyyMM", CultureInfo.InvariantCulture);

        var prefix = $"{kind}-{period}-";
        var next = await DocumentSequence.NextAsync(db, table, "number", prefix, segment: 3, cancellationToken);

        return $"{prefix}{next:D4}";
    }
}
