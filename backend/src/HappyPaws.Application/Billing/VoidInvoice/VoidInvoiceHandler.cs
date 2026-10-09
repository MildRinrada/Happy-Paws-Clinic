using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Billing.VoidInvoice;

/// <summary>
/// <c>POST /staff/invoices/{id}/void</c> — <b>not in the API reference</b>.
/// <para>
/// <c>void</c> is drawn in the contract's invoice state machine and no documented endpoint reaches
/// it, exactly as <c>left</c> was for the queue. Recorded as gap 3 in
/// <c>docs/contract-extensions.md</c> when the Consultation module was built; implemented now that
/// there is an invoice to void.
/// </para>
/// </summary>
public sealed record VoidInvoiceCommand(string InvoiceId, string Reason, uint? IfMatchVersion = null);

public sealed class VoidInvoiceValidator : AbstractValidator<VoidInvoiceCommand>
{
    public VoidInvoiceValidator()
    {
        // Voiding is how a bill disappears from every total the clinic reports. Without a reason on
        // the row, a month that does not add up has nothing to explain it.
        RuleFor(command => command.Reason).NotEmpty().WithMessage("ต้องระบุเหตุผลในการยกเลิกบิล");
    }
}

/// <summary>
/// Cancels a bill raised in error.
/// <para>
/// Scoped to <c>invoice:write</c> rather than a scope of its own. The contract's catalogue is
/// closed — <c>PUT /staff/roles/{id}/permissions</c> rejects unknown scopes — so inventing
/// <c>invoice:void</c> would make this endpoint unreachable by every role the reference defines.
/// The person who may raise a bill may cancel one that has taken no money.
/// </para>
/// </summary>
public sealed class VoidInvoiceHandler(
    InvoiceAccess access,
    IInvoiceRewards rewards,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<InvoiceDto> HandleAsync(
        VoidInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        Invoice? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var invoice = await access.ForStaffAsync(command.InvoiceId, token);

            ConcurrencyGuard.Ensure(invoice, command.IfMatchVersion);

            invoice.Void(command.Reason, staffId, clock.UtcNow);

            // Points, packages and coupons that follow this bill's money. See IInvoiceRewards.
            await rewards.VoidedAsync(invoice, clock.UtcNow, token);

            audit.Record(new AuditEvent(
                AuditActions.InvoiceVoided,
                AuditResources.Invoice,
                invoice.Id,
                invoice.PetId,
                invoice.BranchId,
                new Dictionary<string, object?>
                {
                    ["number"] = invoice.Number,
                    ["total_satang"] = invoice.Total.Satang,
                    ["reason"] = invoice.VoidReason,
                }));

            updated = invoice;
        }, cancellationToken);

        return InvoiceDto.From(updated!);
    }
}
