using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.GetLoyaltyAccount;

/// <summary>
/// <c>GET /staff/loyalty/{customer_id}</c> — "แต้มสะสม ระดับสมาชิก และประวัติแลก".
/// <para>
/// Points, level and history, plus the customer's packages: the contract gives staff no other way
/// to see what a customer has bought, and the counter needs it on the same screen as the points.
/// </para>
/// <para>
/// <b>Clinic-wide.</b> A customer belongs to the clinic, not to a branch (the same as
/// <c>GET /staff/customers/{id}</c>), and points earned at one branch are spent at another, so a
/// branch-bound account sees the whole account.
/// </para>
/// </summary>
public sealed class GetLoyaltyAccountHandler(
    ICustomerRepository customers,
    ILoyaltyLedger ledger,
    ICouponRepository coupons,
    ICustomerPackageRepository packages,
    IClinicSettingsStore settings,
    IClock clock,
    ClinicDays clinicDays)
{
    public const int HistoryLimit = 50;

    public async Task<LoyaltyAccountDto> HandleAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await customers.FindByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("customer", customerId);

        var loyalty = await settings.LoyaltyAsync(cancellationToken);
        var totals = await ledger.TotalsAsync(customer.Id, clock.UtcNow, cancellationToken);
        var history = await ledger.HistoryAsync(customer.Id, HistoryLimit, cancellationToken);
        var redemptions = await coupons.HistoryForCustomerAsync(customer.Id, HistoryLimit, cancellationToken);
        var owned = await packages.ForCustomerAsync(customer.Id, cancellationToken);

        var next = loyalty.NextTier(totals.Lifetime);

        return new LoyaltyAccountDto(
            customer.Id,
            totals.Balance,
            totals.Lifetime,
            CrmWire.ToWire(loyalty.TierFor(totals.Lifetime)),
            next is { } step ? new NextTierDto(CrmWire.ToWire(step.Tier), step.PointsNeeded) : null,
            LoyaltyRulesDto.From(loyalty),
            totals.Expired,
            totals.NextExpiryAt is { } expiresAt ? new NextExpiryDto(totals.NextExpiringPoints, expiresAt) : null,
            [.. history.Select(row => LoyaltyEntryDto.From(row.Entry, row.InvoiceNumber))],
            [.. redemptions.Select(row => new CouponRedemptionDto(
                row.Redemption.Id,
                row.Redemption.CouponId,
                row.Redemption.Code,
                row.Redemption.InvoiceId,
                row.InvoiceNumber,
                row.Redemption.AmountSatang,
                row.Redemption.RedeemedAt,
                row.Redemption.ReleasedAt))],
            PackageDto.Ordered(owned, clinicDays.Today));
    }
}
