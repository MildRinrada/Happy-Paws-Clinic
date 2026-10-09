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

namespace HappyPaws.Application.Billing.CollectPayment;

/// <summary>
/// <c>POST /staff/invoices/{id}/payments</c> —
/// "เงินสด บัตร PromptPay · แบ่งจ่ายหลายวิธีได้".
/// </summary>
public sealed record CollectPaymentCommand(
    string InvoiceId,
    string Method,
    long AmountSatang,
    string? Reference = null,
    uint? IfMatchVersion = null);

public sealed class CollectPaymentValidator : AbstractValidator<CollectPaymentCommand>
{
    public CollectPaymentValidator()
    {
        RuleFor(command => command.Method).NotEmpty();
        RuleFor(command => command.AmountSatang).GreaterThan(0);
    }
}

/// <summary>
/// Takes one tender against a bill.
/// <para>
/// One call is one tender, which is what makes "แบ่งจ่ายหลายวิธีได้" work: paying ฿400 in cash and
/// ฿1,060 on a card is two requests, and the invoice moves from <c>open</c> to
/// <c>partially_paid</c> to <c>paid</c> on its own as they land. The status is never sent by the
/// caller and never assigned here — see <see cref="Invoice"/>.
/// </para>
/// <para>
/// <b>No gateway is called.</b> The contract's <c>PUT /staff/integrations/payment</c> configures a
/// provider that does not exist in this system yet, so a card payment recorded here means "the
/// terminal approved it and the slip number is in <c>reference</c>", not "we charged the card".
/// When the gateway lands it goes in front of this handler, not inside it: authorising a card is a
/// network call, and it must not happen while this transaction holds a row lock on the invoice.
/// </para>
/// </summary>
public sealed class CollectPaymentHandler(
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
        CollectPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var method = BillingWire.ParseMethod(command.Method);

        Invoice? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var invoice = await access.ForStaffAsync(command.InvoiceId, token);

            ConcurrencyGuard.Ensure(invoice, command.IfMatchVersion);

            var payment = invoice.Pay(method, command.AmountSatang, staffId, clock.UtcNow, command.Reference);

            // Points, packages and coupons that follow this bill's money. See IInvoiceRewards.
            await rewards.PaymentCollectedAsync(invoice, clock.UtcNow, token);

            webhooks.Enqueue(WebhookEventTypes.PaymentSucceeded, WebhookPayloads.Payment(invoice, payment));

            audit.Record(new AuditEvent(
                AuditActions.PaymentCollected,
                AuditResources.Invoice,
                invoice.Id,
                invoice.PetId,
                invoice.BranchId,
                new Dictionary<string, object?>
                {
                    ["payment_id"] = payment.Id,
                    ["method"] = BillingWire.ToWire(payment.Method),
                    ["amount_satang"] = payment.AmountSatang,
                    ["balance_satang"] = invoice.Balance.Satang,
                    ["status"] = BillingWire.ToWire(invoice.Status),
                }));

            updated = invoice;
        }, cancellationToken);

        return InvoiceDto.From(updated!);
    }
}
