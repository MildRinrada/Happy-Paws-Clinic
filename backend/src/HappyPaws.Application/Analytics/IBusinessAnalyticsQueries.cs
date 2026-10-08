using HappyPaws.Domain.Analytics;
using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Analytics;

/// <summary>
/// The read model behind "Business dashboard": money, services, customers and people.
/// <para>
/// Money is read from the rows Billing writes and never from a total stored somewhere else. A
/// payment is <c>invoice_payments</c>, a refund is <c>invoice_refunds</c>, and an invoice total is
/// its lines less its discounts, computed the way <c>Invoice</c> computes it. There is no second
/// ledger here that could disagree with the cash desk.
/// </para>
/// </summary>
public interface IBusinessAnalyticsQueries
{
    /// <summary>Payments taken and refunds paid inside the window, by clinic day, branch and tender.</summary>
    Task<IReadOnlyList<RevenueRow>> RevenueAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default);

    /// <summary>What was billed on invoices issued inside the window, voided ones excluded.</summary>
    Task<InvoicedTotal> InvoicedAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The money kept on invoices issued inside the window, shared out over their lines. Service
    /// lines come back one row per service; every other kind comes back as one row per kind.
    /// </summary>
    Task<IReadOnlyList<ServiceRevenueRow>> ServiceRevenueAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default);

    /// <summary>Distinct visiting customers per month of the window, by the month of their first visit.</summary>
    Task<IReadOnlyList<RetentionCell>> RetentionAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default);

    /// <summary>Per staff member: cases, treatments and money inside the window.</summary>
    Task<IReadOnlyList<StaffPerformanceRow>> StaffPerformanceAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default);
}

/// <summary>Money that moved on one clinic day, at one branch, by one tender.</summary>
public sealed record RevenueRow(
    DateOnly Day,
    string BranchId,
    string? BranchName,
    PaymentMethod Method,
    long CollectedSatang,
    int Payments,
    long RefundedSatang,
    int Refunds);

/// <summary>Billed totals: lines less discounts, over invoices that were not voided.</summary>
public sealed record InvoicedTotal(long InvoicedSatang, int Invoices);

/// <summary>
/// One service, or one kind of line that is not a service.
/// </summary>
/// <param name="ServiceId">The catalogue service, when the line can be traced to one.</param>
/// <param name="Name">The service's name, or the line's own text for a service line with no catalogue entry. Null for non-service kinds.</param>
/// <param name="Kind">The line kind.</param>
/// <param name="Quantity">Units billed.</param>
/// <param name="Invoices">Distinct invoices it appeared on.</param>
/// <param name="BilledSatang">Quantity times unit price, before discounts.</param>
/// <param name="RevenueSatang">Its share of what was kept on those invoices: paid less refunded.</param>
/// <param name="MedicineSatang">For a service: the kept money of medicine lines on the same invoices, shared over their service lines.</param>
public sealed record ServiceRevenueRow(
    string? ServiceId,
    string? Name,
    InvoiceLineKind Kind,
    int Quantity,
    int Invoices,
    long BilledSatang,
    long RevenueSatang,
    long MedicineSatang);

/// <summary>One staff member's numbers inside the window.</summary>
/// <param name="StaffId">Staff membership id.</param>
/// <param name="DisplayName">The account's name.</param>
/// <param name="StaffType">The membership's staff type.</param>
/// <param name="Cases">Consultations they closed.</param>
/// <param name="CaseSeconds">Opened to closed, summed over those.</param>
/// <param name="Treatments">Treatment lines they performed.</param>
/// <param name="AttributedRevenueSatang">Payments less refunds on the invoices of the cases they ran.</param>
/// <param name="CollectedSatang">Payments they took at the desk.</param>
public sealed record StaffPerformanceRow(
    string StaffId,
    string DisplayName,
    string? StaffType,
    int Cases,
    double CaseSeconds,
    int Treatments,
    long AttributedRevenueSatang,
    long CollectedSatang);
