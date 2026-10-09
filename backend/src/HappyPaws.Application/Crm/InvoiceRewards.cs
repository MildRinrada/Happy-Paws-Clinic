using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm;

/// <summary>
/// Packages, coupons and points acting on a bill. The rules are written out in
/// <c>docs/features/crm.md</c>; in short:
/// <list type="bullet">
/// <item><b>Packages</b> are spent automatically when a covered catalogue service is billed. The
/// covered quantity goes on the bill as a line priced zero, so revenue is counted once, when the
/// package itself was paid for.</item>
/// <item><b>Coupons</b> become a discount whose approver is the staff member who created the coupon.</item>
/// <item><b>Points</b> are earned when a bill reaches <c>paid</c> (one per ฿100 paid), taken back in
/// proportion when money is refunded, and spent as a discount at ฿1 a point.</item>
/// <item>A <b>void</b> gives back everything the bill took: package uses, coupon uses and points.
/// A <b>refund</b> does not, because the service and the discount happened.</item>
/// </list>
/// </summary>
public sealed class InvoiceRewards(
    ICustomerPackageRepository packages,
    ICouponRepository coupons,
    ILoyaltyLedger ledger,
    ICrmLocks locks,
    IBilledServiceLookup billedServices,
    IAuditTrail audit,
    ClinicDays clinicDays) : IInvoiceRewards
{
    public async Task AddServiceLineAsync(
        Invoice invoice,
        ServiceLineRequest line,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var remaining = line.Quantity;

        // A service that costs nothing is not worth a session of anybody's package.
        if (line.UnitPriceSatang > 0)
        {
            await locks.LockCustomerAsync(invoice.CustomerId, cancellationToken);

            var today = clinicDays.DateOf(now);
            var candidates = (await packages.SpendableForCustomerTrackedAsync(invoice.CustomerId, cancellationToken))
                .Where(package => package.Covers(line.ServiceId, invoice.PetId) && package.IsUsableOn(today))
                .OrderBy(package => package.ExpiresOn ?? DateOnly.MaxValue)
                .ThenBy(package => package.PurchasedAt)
                .ToList();

            foreach (var package in candidates)
            {
                var take = Math.Min(remaining, package.Remaining);

                if (take <= 0)
                {
                    continue;
                }

                package.Use(take, today);

                var covered = invoice.AddLine(
                    InvoiceLineKind.Service,
                    $"{line.Description} (ใช้สิทธิ์แพ็กเกจ {package.Name})",
                    take,
                    0,
                    line.SourceId);

                packages.AddUse(PackageUse.Record(package, invoice.Id, covered.Id, line.ServiceId, invoice.PetId, take, now));

                audit.Record(new AuditEvent(
                    AuditActions.PackageUsed,
                    AuditResources.Package,
                    package.Id,
                    invoice.PetId,
                    invoice.BranchId,
                    new Dictionary<string, object?>
                    {
                        ["invoice_id"] = invoice.Id,
                        ["service_id"] = line.ServiceId,
                        ["quantity"] = take,
                        ["remaining"] = package.Remaining,
                    }));

                remaining -= take;

                if (remaining == 0)
                {
                    break;
                }
            }
        }

        if (remaining > 0)
        {
            invoice.AddLine(InvoiceLineKind.Service, line.Description, remaining, line.UnitPriceSatang, line.SourceId);
        }
    }

    public async Task<InvoiceDiscount> RedeemCouponAsync(
        Invoice invoice,
        string couponCode,
        string staffId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        EnsureDiscountable(invoice);

        var code = Coupon.NormalizeCode(couponCode);
        var couponId = await coupons.IdForCodeAsync(code, cancellationToken)
            ?? throw new NotFoundException("coupon", code);

        // Under the coupon's lock, and read after taking it: a second cashier redeeming the last
        // use of a single-use code waits here, then reads the count the first one committed.
        await locks.LockCouponAsync(couponId, cancellationToken);

        var coupon = await coupons.FindTrackedAsync(couponId, cancellationToken)
            ?? throw new NotFoundException("coupon", code);

        if (await coupons.IsRedeemedOnInvoiceAsync(coupon.Id, invoice.Id, cancellationToken))
        {
            throw new DomainException("coupon_not_applicable", "ใช้คูปองนี้กับบิลนี้ไปแล้ว");
        }

        var usedByCustomer = await coupons.StandingRedemptionsAsync(coupon.Id, invoice.CustomerId, cancellationToken);
        coupon.EnsureRedeemable(clinicDays.DateOf(now), invoice.BranchId, usedByCustomer);

        var eligible = await EligibleAmountAsync(invoice, coupon, cancellationToken);
        var amount = Math.Min(coupon.DiscountFor(eligible), invoice.Total.Satang - invoice.Paid.Satang);

        if (amount <= 0)
        {
            throw new DomainException("coupon_not_applicable", "บิลนี้ไม่มีรายการที่คูปองนี้ใช้ได้ หรือไม่มียอดเหลือให้ลด");
        }

        var discount = invoice.ApplyDiscount(
            amount,
            $"คูปอง {coupon.Code} ({coupon.Name})",
            coupon.CreatedByStaffId,
            staffId,
            now);

        coupon.Redeem();
        coupons.AddRedemption(CouponRedemption.Record(coupon, invoice.Id, invoice.CustomerId, discount.Id, amount, staffId, now));

        return discount;
    }

    public async Task<InvoiceDiscount> RedeemPointsAsync(
        Invoice invoice,
        int points,
        string staffId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        EnsureDiscountable(invoice);

        // Two bills spending the same points at once would both see the old balance without this.
        await locks.LockCustomerAsync(invoice.CustomerId, cancellationToken);

        var totals = await ledger.TotalsAsync(invoice.CustomerId, cancellationToken);

        if (points > totals.Balance)
        {
            throw new DomainException("insufficient_points", $"แต้มไม่พอ ลูกค้ามี {Math.Max(totals.Balance, 0):N0} แต้ม");
        }

        var amount = points * LoyaltyRules.PointValueSatang;
        var room = invoice.Total.Satang - invoice.Paid.Satang;

        if (amount > room)
        {
            throw new DomainException(
                "invalid_discount",
                $"แลกแต้มได้ไม่เกินยอดค้างของบิล (สูงสุด {room / LoyaltyRules.PointValueSatang:N0} แต้ม)");
        }

        var discount = invoice.ApplyDiscount(amount, $"แลกแต้มสะสม {points:N0} แต้ม", staffId, staffId, now);

        ledger.Add(LoyaltyEntry.Create(
            invoice.CustomerId, LoyaltyEntryKind.Redeem, points, now, invoice.Id, discount.Id, invoice.Number, staffId));

        return discount;
    }

    public async Task PaymentCollectedAsync(Invoice invoice, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (invoice.Status != InvoiceStatus.Paid)
        {
            return;
        }

        var entries = await ledger.ForInvoiceAsync(invoice.Id, cancellationToken);

        if (entries.All(entry => entry.Kind != LoyaltyEntryKind.Earn))
        {
            var earned = LoyaltyRules.PointsFor(invoice.Paid.Satang);

            if (earned > 0)
            {
                ledger.Add(LoyaltyEntry.Create(invoice.CustomerId, LoyaltyEntryKind.Earn, earned, now, invoice.Id, note: invoice.Number));
            }
        }

        foreach (var package in await packages.SoldOnInvoiceTrackedAsync(invoice.Id, cancellationToken))
        {
            package.Activate(now);
        }
    }

    public async Task RefundedAsync(Invoice invoice, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var entries = await ledger.ForInvoiceAsync(invoice.Id, cancellationToken);

        if (entries.Any(entry => entry.Kind == LoyaltyEntryKind.Earn))
        {
            // Points follow the money the clinic kept: a bill of ฿1,000 refunded ฿300 keeps 7 points.
            var held = entries.Where(entry => entry.Kind is LoyaltyEntryKind.Earn or LoyaltyEntryKind.Reverse).Sum(entry => entry.Points);
            var kept = LoyaltyRules.PointsFor(invoice.Paid.Satang - invoice.Refunded.Satang);

            if (held > kept)
            {
                ledger.Add(LoyaltyEntry.Create(
                    invoice.CustomerId, LoyaltyEntryKind.Reverse, held - kept, now, invoice.Id, note: invoice.Number));
            }
        }

        await CancelSoldPackagesAsync(invoice, "คืนเงินบิลขายแพ็กเกจ", now, cancellationToken);
    }

    public async Task VoidedAsync(Invoice invoice, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await locks.LockCustomerAsync(invoice.CustomerId, cancellationToken);

        foreach (var use in await packages.OpenUsesForInvoiceTrackedAsync(invoice.Id, cancellationToken))
        {
            var package = await packages.FindTrackedAsync(use.PackageId, cancellationToken);
            package?.Release(use.Quantity);
            use.Release(now);
        }

        foreach (var redemption in await coupons.OpenRedemptionsForInvoiceTrackedAsync(invoice.Id, cancellationToken))
        {
            await locks.LockCouponAsync(redemption.CouponId, cancellationToken);

            var coupon = await coupons.FindTrackedAsync(redemption.CouponId, cancellationToken);
            coupon?.Release();
            redemption.Release(now);
        }

        var entries = await ledger.ForInvoiceAsync(invoice.Id, cancellationToken);
        var spent = -entries.Where(entry => entry.Kind is LoyaltyEntryKind.Redeem or LoyaltyEntryKind.Restore).Sum(entry => entry.Points);

        if (spent > 0)
        {
            ledger.Add(LoyaltyEntry.Create(
                invoice.CustomerId, LoyaltyEntryKind.Restore, spent, now, invoice.Id, note: invoice.Number));
        }

        await CancelSoldPackagesAsync(invoice, "ยกเลิกบิลขายแพ็กเกจ", now, cancellationToken);
    }

    private async Task CancelSoldPackagesAsync(Invoice invoice, string reason, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var package in await packages.SoldOnInvoiceTrackedAsync(invoice.Id, cancellationToken))
        {
            if (package.CancelledAt is not null)
            {
                continue;
            }

            package.Cancel(reason, now);

            audit.Record(new AuditEvent(
                AuditActions.PackageCancelled,
                AuditResources.Package,
                package.Id,
                package.PetId,
                package.BranchId,
                new Dictionary<string, object?>
                {
                    ["invoice_id"] = invoice.Id,
                    ["reason"] = reason,
                    ["used_count"] = package.UsedCount,
                    ["total_uses"] = package.TotalUses,
                }));
        }
    }

    /// <summary>The part of the bill a coupon applies to: the whole subtotal, or its listed services' lines.</summary>
    private async Task<long> EligibleAmountAsync(Invoice invoice, Coupon coupon, CancellationToken cancellationToken)
    {
        if (coupon.ServiceIds.Count == 0)
        {
            return invoice.Subtotal.Satang;
        }

        var services = await billedServices.ServiceIdsByLineAsync(invoice.Id, cancellationToken);

        return invoice.Lines
            .Where(line => services.TryGetValue(line.Id, out var serviceId) && coupon.ServiceIds.Contains(serviceId))
            .Sum(line => line.LineTotal.Satang);
    }

    private static void EnsureDiscountable(Invoice invoice)
    {
        if (invoice.Status is not (InvoiceStatus.Open or InvoiceStatus.PartiallyPaid))
        {
            throw new StateConflictException(nameof(Invoice), invoice.Status.ToString(), nameof(Invoice.ApplyDiscount));
        }
    }
}
