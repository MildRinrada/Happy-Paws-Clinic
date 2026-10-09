using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Billing.RefundPayment;

/// <summary>
/// <c>POST /staff/invoices/{id}/refunds</c> —
/// "คืนเงินบางส่วนหรือทั้งใบ · ผูกเหตุผล".
/// </summary>
public sealed record RefundPaymentCommand(
    string InvoiceId,
    long AmountSatang,
    string Reason,
    string? Method = null,
    string? PaymentId = null,
    uint? IfMatchVersion = null);

public sealed class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(command => command.AmountSatang).GreaterThan(0);

        // "ผูกเหตุผล". Enforced at both layers: here so the caller is told which field, and in the
        // aggregate so no other path can write a refund without one.
        RuleFor(command => command.Reason).NotEmpty().WithMessage("ต้องระบุเหตุผลในการคืนเงิน");
    }
}

/// <summary>
/// Gives money back, against a bill that took some.
/// <para>
/// The method defaults to the tender being reversed, or to the invoice's single tender when there
/// is only one. Refunding a card payment as cash is a real thing a counter sometimes does, so it
/// stays possible — but it has to be said out loud, because the default is the one that reconciles.
/// </para>
/// <para>
/// A refund ends the bill: it moves to <c>refunded</c> and stays there, partial or not. The
/// contract draws no state for "paid, then partly given back", and the alternative reading — back
/// to <c>partially_paid</c> — would put a settled bill back on the counter's chase list. See
/// <c>docs/assumptions.md § 22</c>.
/// </para>
/// </summary>
public sealed class RefundPaymentHandler(
    InvoiceAccess access,
    IInvoiceRewards rewards,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<InvoiceDto> HandleAsync(
        RefundPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        Invoice? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var invoice = await access.ForStaffAsync(command.InvoiceId, token);

            ConcurrencyGuard.Ensure(invoice, command.IfMatchVersion);

            var refund = invoice.Refund(
                ResolveMethod(command, invoice),
                command.AmountSatang,
                command.Reason,
                staffId,
                clock.UtcNow,
                command.PaymentId);

            // Points, packages and coupons that follow this bill's money. See IInvoiceRewards.
            await rewards.RefundedAsync(invoice, clock.UtcNow, token);

            webhooks.Enqueue(WebhookEventTypes.PaymentRefunded, WebhookPayloads.Refund(invoice, refund));

            audit.Record(new AuditEvent(
                AuditActions.PaymentRefunded,
                AuditResources.Invoice,
                invoice.Id,
                invoice.PetId,
                invoice.BranchId,
                new Dictionary<string, object?>
                {
                    ["refund_id"] = refund.Id,
                    ["payment_id"] = refund.PaymentId,
                    ["method"] = BillingWire.ToWire(refund.Method),
                    ["amount_satang"] = refund.AmountSatang,
                    ["reason"] = refund.Reason,
                    ["refunded_total_satang"] = invoice.Refunded.Satang,
                }));

            updated = invoice;
        }, cancellationToken);

        return InvoiceDto.From(updated!);
    }

    /// <summary>Stated method, else the tender being reversed, else the bill's only tender, else cash.</summary>
    private static PaymentMethod ResolveMethod(RefundPaymentCommand command, Invoice invoice)
    {
        if (command.Method is { Length: > 0 })
        {
            return BillingWire.ParseMethod(command.Method);
        }

        if (command.PaymentId is { Length: > 0 } paymentId)
        {
            var payment = invoice.Payments.FirstOrDefault(candidate => candidate.Id == paymentId);

            if (payment is not null)
            {
                return payment.Method;
            }
        }

        var methods = invoice.Payments.Select(payment => payment.Method).Distinct().ToList();

        return methods.Count == 1 ? methods[0] : PaymentMethod.Cash;
    }
}
