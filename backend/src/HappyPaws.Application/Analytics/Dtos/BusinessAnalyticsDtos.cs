namespace HappyPaws.Application.Analytics.Dtos;

/// <summary><c>GET /staff/analytics/revenue</c>. Cash basis: money on the day it moved.</summary>
/// <param name="From">First clinic day counted.</param>
/// <param name="To">Last clinic day counted.</param>
/// <param name="BranchId">The branch asked for, if one was.</param>
/// <param name="Granularity">How <paramref name="Series"/> is grouped.</param>
/// <param name="Totals">The whole span.</param>
/// <param name="ByMethod">Every tender, including those with nothing on them.</param>
/// <param name="ByBranch">Every branch money moved at.</param>
/// <param name="Series">One row per period, empty periods included.</param>
public sealed record RevenueAnalyticsDto(
    DateOnly From,
    DateOnly To,
    string? BranchId,
    string Granularity,
    RevenueTotalsDto Totals,
    IReadOnlyList<RevenueByMethodDto> ByMethod,
    IReadOnlyList<RevenueByBranchDto> ByBranch,
    IReadOnlyList<RevenuePeriodDto> Series);

/// <param name="CollectedSatang">Payments taken.</param>
/// <param name="RefundedSatang">Refunds paid.</param>
/// <param name="NetSatang">Collected less refunded.</param>
/// <param name="Payments">How many payments.</param>
/// <param name="Refunds">How many refunds.</param>
/// <param name="InvoicedSatang">Billed on invoices issued in the span, voided ones excluded: lines less discounts.</param>
/// <param name="Invoices">How many such invoices.</param>
public sealed record RevenueTotalsDto(
    long CollectedSatang,
    long RefundedSatang,
    long NetSatang,
    int Payments,
    int Refunds,
    long InvoicedSatang,
    int Invoices);

/// <param name="Method"><c>cash</c>, <c>card</c> or <c>promptpay</c>.</param>
/// <param name="CollectedSatang">Payments taken by this tender.</param>
/// <param name="RefundedSatang">Refunds paid out by this tender.</param>
/// <param name="NetSatang">Collected less refunded.</param>
public sealed record RevenueByMethodDto(string Method, long CollectedSatang, long RefundedSatang, long NetSatang);

/// <param name="BranchId">Branch the invoice belongs to.</param>
/// <param name="BranchName">Its name.</param>
/// <param name="CollectedSatang">Payments taken.</param>
/// <param name="RefundedSatang">Refunds paid.</param>
/// <param name="NetSatang">Collected less refunded.</param>
public sealed record RevenueByBranchDto(
    string BranchId,
    string? BranchName,
    long CollectedSatang,
    long RefundedSatang,
    long NetSatang);

/// <param name="Period">First day of the period.</param>
/// <param name="CollectedSatang">Payments taken.</param>
/// <param name="RefundedSatang">Refunds paid.</param>
/// <param name="NetSatang">Collected less refunded.</param>
public sealed record RevenuePeriodDto(DateOnly Period, long CollectedSatang, long RefundedSatang, long NetSatang);

/// <summary><c>GET /staff/analytics/services</c>.</summary>
/// <param name="From">First clinic day counted.</param>
/// <param name="To">Last clinic day counted.</param>
/// <param name="BranchId">The branch asked for, if one was.</param>
/// <param name="Services">One row per service, most revenue first.</param>
/// <param name="ByKind">The same money split by line kind; the kinds add up to everything kept.</param>
public sealed record ServiceAnalyticsDto(
    DateOnly From,
    DateOnly To,
    string? BranchId,
    IReadOnlyList<ServiceRevenueDto> Services,
    IReadOnlyList<LineKindRevenueDto> ByKind);

/// <param name="ServiceId">Catalogue service; absent for a service line typed by hand.</param>
/// <param name="Name">Service name.</param>
/// <param name="Quantity">Units billed.</param>
/// <param name="Invoices">Invoices it was on.</param>
/// <param name="BilledSatang">List price times quantity, before discounts.</param>
/// <param name="RevenueSatang">Its share of what was kept: payments less refunds.</param>
/// <param name="MedicineSatang">Medicine sold on the same invoices, shared over their services.</param>
/// <param name="RevenueExcludingMedicineSatang">
/// <paramref name="RevenueSatang"/> less <paramref name="MedicineSatang"/>: "หลังหักยา" as far as the
/// data can say it. A real margin needs the medicine's cost, which arrives with Inventory.
/// </param>
public sealed record ServiceRevenueDto(
    string? ServiceId,
    string Name,
    int Quantity,
    int Invoices,
    long BilledSatang,
    long RevenueSatang,
    long MedicineSatang,
    long RevenueExcludingMedicineSatang);

/// <param name="Kind"><c>service</c>, <c>treatment</c>, <c>medicine</c> or <c>other</c>.</param>
/// <param name="BilledSatang">Before discounts.</param>
/// <param name="RevenueSatang">Share of what was kept.</param>
public sealed record LineKindRevenueDto(string Kind, long BilledSatang, long RevenueSatang);

/// <summary><c>GET /staff/analytics/retention</c>.</summary>
/// <param name="From">First day of the first month counted.</param>
/// <param name="To">Last clinic day counted.</param>
/// <param name="BranchId">The branch asked for, if one was.</param>
/// <param name="Months">New and returning customers, month by month.</param>
/// <param name="Cohorts">Each month's new customers and how many came back in each later month.</param>
public sealed record RetentionAnalyticsDto(
    DateOnly From,
    DateOnly To,
    string? BranchId,
    IReadOnlyList<RetentionMonthDto> Months,
    IReadOnlyList<RetentionCohortDto> Cohorts);

/// <param name="Month">First day of the month.</param>
/// <param name="Active">Distinct customers who visited.</param>
/// <param name="New">Of those, first visit ever.</param>
/// <param name="Returning">Of those, visited before.</param>
public sealed record RetentionMonthDto(DateOnly Month, int Active, int New, int Returning);

/// <param name="Cohort">First day of the month of their first visit.</param>
/// <param name="Customers">How many started that month.</param>
/// <param name="Retained">Of them, how many visited in month 0, 1, 2 and so on after it.</param>
/// <param name="RetentionRates">The same as fractions of <paramref name="Customers"/>.</param>
public sealed record RetentionCohortDto(
    DateOnly Cohort,
    int Customers,
    IReadOnlyList<int> Retained,
    IReadOnlyList<decimal?> RetentionRates);

/// <summary><c>GET /staff/analytics/staff-performance</c>.</summary>
/// <param name="From">First clinic day counted.</param>
/// <param name="To">Last clinic day counted.</param>
/// <param name="BranchId">The branch asked for, if one was.</param>
/// <param name="Staff">One row per person, most cases first.</param>
public sealed record StaffPerformanceDto(
    DateOnly From,
    DateOnly To,
    string? BranchId,
    IReadOnlyList<StaffPerformanceEntryDto> Staff);

/// <param name="StaffId">Staff membership id.</param>
/// <param name="DisplayName">Name.</param>
/// <param name="StaffType">Staff type.</param>
/// <param name="Cases">Consultations closed.</param>
/// <param name="AverageCaseMinutes">Opened to closed.</param>
/// <param name="Treatments">Treatment lines performed.</param>
/// <param name="AttributedRevenueSatang">Payments less refunds on the invoices of their cases.</param>
/// <param name="CollectedSatang">Payments they took at the desk.</param>
public sealed record StaffPerformanceEntryDto(
    string StaffId,
    string DisplayName,
    string? StaffType,
    int Cases,
    decimal? AverageCaseMinutes,
    int Treatments,
    long AttributedRevenueSatang,
    long CollectedSatang);
