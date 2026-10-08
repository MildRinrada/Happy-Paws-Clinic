using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Application.Billing;
using HappyPaws.Domain.Analytics;
using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Analytics.GetRevenueAnalytics;

/// <summary><c>GET /staff/analytics/revenue?from=&amp;to=&amp;branch=&amp;granularity=</c>.</summary>
/// <param name="From">First clinic day; 29 days before <paramref name="To"/> when absent.</param>
/// <param name="To">Last clinic day, included; today when absent.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
/// <param name="Granularity"><c>day</c> (default), <c>week</c> or <c>month</c>.</param>
public sealed record GetRevenueAnalyticsQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? BranchId = null,
    string? Granularity = null);

/// <summary>
/// "รายวัน/เดือน แยกสาขาและช่องทางชำระ".
/// <para>
/// <b>Cash basis.</b> Revenue is money that actually moved: each payment on the clinic day it was
/// taken, less each refund on the clinic day it was paid out. That is the number the drawer and the
/// card terminal agree with at the end of a day, and the only one that stays true under Billing's
/// rules: a void can only happen before any money is taken, so voids never touch it; a partial
/// refund leaves the bill <c>refunded</c> but only its own amount is subtracted; a bill paid in two
/// tenders shows up under both.
/// </para>
/// <para>
/// A refund is counted by its own tender and on its own day, not the original payment's. A card
/// payment refunded in cash a week later is ฿0 net on neither day, and the cash drawer is short on
/// the second one; that is what happened.
/// </para>
/// <para>
/// <c>invoiced_satang</c> is the other basis, kept beside it for comparison: what was billed on
/// invoices issued in the span, lines less discounts, voided bills excluded. The two differ by
/// bills not yet paid and by payments for bills from another span.
/// </para>
/// </summary>
public sealed class GetRevenueAnalyticsHandler(IBusinessAnalyticsQueries business, ReportWindows windows)
{
    public const int DefaultDays = 30;

    private static readonly PaymentMethod[] Tenders = [PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.PromptPay];

    public async Task<RevenueAnalyticsDto> HandleAsync(
        GetRevenueAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        var granularity = AnalyticsWire.ParseGranularity(query.Granularity, ReportGranularity.Day);
        var window = windows.Resolve(query.From, query.To, query.BranchId, DefaultDays);

        var rows = await business.RevenueAsync(window, cancellationToken);
        var invoiced = await business.InvoicedAsync(window, cancellationToken);

        var collected = rows.Sum(row => row.CollectedSatang);
        var refunded = rows.Sum(row => row.RefundedSatang);

        var totals = new RevenueTotalsDto(
            collected,
            refunded,
            collected - refunded,
            rows.Sum(row => row.Payments),
            rows.Sum(row => row.Refunds),
            invoiced.InvoicedSatang,
            invoiced.Invoices);

        var byMethod = Tenders
            .Select(method =>
            {
                var set = rows.Where(row => row.Method == method).ToList();
                var inSum = set.Sum(row => row.CollectedSatang);
                var outSum = set.Sum(row => row.RefundedSatang);

                return new RevenueByMethodDto(BillingWire.ToWire(method), inSum, outSum, inSum - outSum);
            })
            .ToList();

        var byBranch = rows
            .GroupBy(row => row.BranchId, StringComparer.Ordinal)
            .Select(group =>
            {
                var inSum = group.Sum(row => row.CollectedSatang);
                var outSum = group.Sum(row => row.RefundedSatang);

                return new RevenueByBranchDto(group.Key, group.First().BranchName, inSum, outSum, inSum - outSum);
            })
            .OrderByDescending(branch => branch.NetSatang)
            .ThenBy(branch => branch.BranchId, StringComparer.Ordinal)
            .ToList();

        var series = ReportPeriods.Between(window.First, window.Last, granularity)
            .Select(period =>
            {
                var set = rows.Where(row => ReportPeriods.StartOf(row.Day, granularity) == period).ToList();
                var inSum = set.Sum(row => row.CollectedSatang);
                var outSum = set.Sum(row => row.RefundedSatang);

                return new RevenuePeriodDto(period, inSum, outSum, inSum - outSum);
            })
            .ToList();

        return new RevenueAnalyticsDto(
            window.First,
            window.Last,
            window.BranchId,
            AnalyticsWire.ToWire(granularity),
            totals,
            byMethod,
            byBranch,
            series);
    }
}
