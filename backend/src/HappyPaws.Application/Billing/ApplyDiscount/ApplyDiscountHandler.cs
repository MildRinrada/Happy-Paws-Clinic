using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Billing.ApplyDiscount;

/// <summary>
/// <c>POST /staff/invoices/{id}/discounts</c> — "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ".
/// <para>
/// Exactly one of four kinds: an amount, a percentage, a coupon code
/// (<c>POST /staff/coupons</c>, "โค้ดส่วนลด") or loyalty points. A request carrying two is a request
/// whose author disagrees with itself, and picking one would silently apply a number the caller did
/// not intend. A coupon or points discount fills in its own reason and approver; see
/// <see cref="IInvoiceRewards"/>.
/// </para>
/// </summary>
public sealed record ApplyDiscountCommand(
    string InvoiceId,
    string? Reason,
    string? ApprovedByStaffId,
    long? AmountSatang = null,
    decimal? Percent = null,
    uint? IfMatchVersion = null,
    string? CouponCode = null,
    int? Points = null)
{
    /// <summary>A discount typed in at the counter, as opposed to a coupon or points.</summary>
    public bool IsManual => CouponCode is null && Points is null;
}

public sealed class ApplyDiscountValidator : AbstractValidator<ApplyDiscountCommand>
{
    public ApplyDiscountValidator()
    {
        RuleFor(command => command)
            .Must(command =>
                (command.AmountSatang.HasValue ? 1 : 0) + (command.Percent.HasValue ? 1 : 0)
                + (command.CouponCode is not null ? 1 : 0) + (command.Points.HasValue ? 1 : 0) == 1)
            .WithMessage("ระบุ amount_satang, percent, coupon_code หรือ points อย่างใดอย่างหนึ่ง");

        When(command => command.IsManual, () =>
        {
            RuleFor(command => command.Reason).NotEmpty().WithMessage("ต้องระบุเหตุผลของส่วนลด");

            // The contract's own words: "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ". Neither is optional, and the
            // approver is required as a field rather than taken from the caller — the person keying a
            // discount in is very often not the person entitled to approve it.
            RuleFor(command => command.ApprovedByStaffId).NotEmpty().WithMessage("ต้องระบุผู้อนุมัติส่วนลด");
        });

        RuleFor(command => command.CouponCode)
            .NotEmpty().WithMessage("ต้องระบุโค้ดคูปอง")
            .MaximumLength(20)
            .When(command => command.CouponCode is not null);

        RuleFor(command => command.Points)
            .InclusiveBetween(1, 1_000_000).WithMessage("จำนวนแต้มต้องอย่างน้อย 1 แต้ม")
            .When(command => command.Points.HasValue);
    }
}

/// <summary>
/// Reduces a bill, on the record.
/// <para>
/// A percentage is resolved against the <b>subtotal</b>, not the running total. "10% off" said at a
/// counter means ten percent of the bill, and compounding it onto whatever an earlier discount left
/// behind would quietly give away less than the approver agreed to — a difference nobody notices
/// until two discounts land on the same invoice.
/// </para>
/// </summary>
public sealed class ApplyDiscountHandler(
    InvoiceAccess access,
    IInvoiceRewards rewards,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<InvoiceDto> HandleAsync(
        ApplyDiscountCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        Invoice? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var invoice = await access.ForStaffAsync(command.InvoiceId, token);

            ConcurrencyGuard.Ensure(invoice, command.IfMatchVersion);

            var (discount, source) = await ApplyAsync(invoice, command, staffId, token);

            audit.Record(new AuditEvent(
                AuditActions.InvoiceDiscounted,
                AuditResources.Invoice,
                invoice.Id,
                invoice.PetId,
                invoice.BranchId,
                new Dictionary<string, object?>
                {
                    ["discount_id"] = discount.Id,
                    ["amount_satang"] = discount.AmountSatang,
                    ["percent"] = discount.Percent,
                    ["reason"] = discount.Reason,
                    ["approved_by_staff_id"] = discount.ApprovedByStaffId,
                    ["total_satang"] = invoice.Total.Satang,
                    ["source"] = source,
                    ["coupon_code"] = command.CouponCode,
                    ["points"] = command.Points,
                }));

            updated = invoice;
        }, cancellationToken);

        return InvoiceDto.From(updated!);
    }

    private async Task<(InvoiceDiscount Discount, string Source)> ApplyAsync(
        Invoice invoice,
        ApplyDiscountCommand command,
        string staffId,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        if (command.CouponCode is { } code)
        {
            return (await rewards.RedeemCouponAsync(invoice, code, staffId, now, cancellationToken), "coupon");
        }

        if (command.Points is { } points)
        {
            return (await rewards.RedeemPointsAsync(invoice, points, staffId, now, cancellationToken), "points");
        }

        var amount = command.AmountSatang
            ?? InvoiceDiscount.ResolvePercent(command.Percent!.Value, invoice.Subtotal.Satang);

        var discount = invoice.ApplyDiscount(
            amount,
            command.Reason!,
            command.ApprovedByStaffId!,
            staffId,
            now,
            command.Percent);

        return (discount, "manual");
    }
}
