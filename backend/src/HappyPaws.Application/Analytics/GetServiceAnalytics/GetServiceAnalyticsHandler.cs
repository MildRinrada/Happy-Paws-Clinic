using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Application.Billing;
using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Analytics.GetServiceAnalytics;

/// <summary><c>GET /staff/analytics/services?from=&amp;to=&amp;branch=</c>.</summary>
/// <param name="From">First clinic day; 29 days before <paramref name="To"/> when absent.</param>
/// <param name="To">Last clinic day, included; today when absent.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
public sealed record GetServiceAnalyticsQuery(DateOnly? From = null, DateOnly? To = null, string? BranchId = null);

/// <summary>
/// "บริการทำเงิน · กำไรต่อบริการหลังหักยา".
/// <para>
/// <b>Which bills.</b> Invoices <i>issued</i> in the span, voided ones excluded. A service earns on
/// the visit it was performed on, so the bill's date is the right one here even though the revenue
/// report uses the payment's date.
/// </para>
/// <para>
/// <b>How much.</b> What the clinic kept on each bill (payments less refunds) shared over its lines
/// in proportion to their list price. Discounts and refunds are recorded per bill, not per line, so
/// a pro-rata share is the only split the data supports, and it has the property that matters: the
/// lines of a bill always add back up to what the bill kept. An unpaid bill has kept nothing yet.
/// </para>
/// <para>
/// <b>หลังหักยา.</b> Medicine on the same bills is shared over that bill's services by the same
/// proportion and reported beside each service, together with the service's revenue less it. That
/// is as close to "profit after medicine" as the data goes: a true margin needs what the medicine
/// <i>cost</i>, which arrives with Inventory.
/// </para>
/// </summary>
public sealed class GetServiceAnalyticsHandler(IBusinessAnalyticsQueries business, ReportWindows windows)
{
    public const int DefaultDays = 30;

    private static readonly InvoiceLineKind[] Kinds =
        [InvoiceLineKind.Service, InvoiceLineKind.Treatment, InvoiceLineKind.Medicine, InvoiceLineKind.Other];

    public async Task<ServiceAnalyticsDto> HandleAsync(
        GetServiceAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        var window = windows.Resolve(query.From, query.To, query.BranchId, DefaultDays);
        var rows = await business.ServiceRevenueAsync(window, cancellationToken);

        var services = rows
            .Where(row => row.Kind == InvoiceLineKind.Service)
            .Select(row => new ServiceRevenueDto(
                row.ServiceId,
                row.Name ?? row.ServiceId ?? "-",
                row.Quantity,
                row.Invoices,
                row.BilledSatang,
                row.RevenueSatang,
                row.MedicineSatang,
                row.RevenueSatang - row.MedicineSatang))
            .OrderByDescending(service => service.RevenueSatang)
            .ThenBy(service => service.Name, StringComparer.Ordinal)
            .ToList();

        var byKind = Kinds
            .Select(kind =>
            {
                var set = rows.Where(row => row.Kind == kind).ToList();

                return new LineKindRevenueDto(
                    BillingWire.ToWire(kind), set.Sum(row => row.BilledSatang), set.Sum(row => row.RevenueSatang));
            })
            .ToList();

        return new ServiceAnalyticsDto(window.First, window.Last, window.BranchId, services, byKind);
    }
}
