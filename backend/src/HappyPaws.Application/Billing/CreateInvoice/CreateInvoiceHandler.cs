using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Billing.CreateInvoice;

/// <summary><c>POST /staff/invoices</c> — "สร้างเองหรือถูกสร้างอัตโนมัติตอนปิดเคส".</summary>
public sealed record CreateInvoiceCommand(
    string CustomerId,
    IReadOnlyList<InvoiceLineInput> Lines,
    string? PetId = null,
    string? ConsultationId = null,
    string? BranchId = null,
    string? Note = null);

/// <summary>One row on a bill raised by hand.</summary>
public sealed record InvoiceLineInput(
    string Description,
    int Quantity,
    long UnitPriceSatang,
    string? Kind = null,
    string? SourceId = null);

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();

        // An empty bill would total zero and be born already `paid`, which is a document that says
        // a customer settled something nobody ever charged them for.
        RuleFor(command => command.Lines).NotEmpty().WithMessage("บิลต้องมีอย่างน้อยหนึ่งรายการ");

        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(input => input.Description).NotEmpty();
            line.RuleFor(input => input.Quantity).GreaterThan(0);
            line.RuleFor(input => input.UnitPriceSatang).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>
/// Raises a bill at the counter.
/// <para>
/// The same aggregate the automatic path builds, reached through a different door — a boarding
/// night, a bag of prescription food, a bill reissued after a void. What it will not do is raise a
/// second bill for a case that already has one: the consultation is checked first, and a duplicate
/// is refused rather than silently doubling a customer's charges.
/// </para>
/// </summary>
public sealed class CreateInvoiceHandler(
    IInvoiceRepository invoices,
    IDocumentNumberGenerator numbers,
    ICustomerRepository customers,
    IBookingCatalog catalog,
    IInvoiceRewards rewards,
    IWebhookOutbox webhooks,
    IAuditTrail audit,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<InvoiceDto> HandleAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;

        Invoice? invoice = null;

        // Loaded inside the transaction: a retry resets the change tracker — see IUnitOfWork.
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var customer = await customers.FindByIdAsync(command.CustomerId, token)
                ?? throw new NotFoundException("customer", command.CustomerId);

            var branchId = await ResolveBranchAsync(command.BranchId, token);
            branchGuard.EnsureCanAccess(branchId);

            await EnsureCaseHasNoInvoiceAsync(command.ConsultationId, token);

            invoice = Invoice.Raise(
                await numbers.NextInvoiceNumberAsync(now, token),
                branchId,
                customer.Id,
                staffId,
                now,
                command.PetId,
                command.ConsultationId,
                command.Note);

            foreach (var line in command.Lines)
            {
                var kind = BillingWire.ParseLineKind(line.Kind);

                if (kind == InvoiceLineKind.Service && IsCatalogueService(line.SourceId))
                {
                    // A service picked from the catalogue: a package the customer holds for it is
                    // spent here, the same as on a bill raised when a case closes.
                    await rewards.AddServiceLineAsync(
                        invoice,
                        new ServiceLineRequest(line.SourceId!, line.Description, line.Quantity, line.UnitPriceSatang, line.SourceId),
                        now,
                        token);
                }
                else
                {
                    invoice.AddLine(kind, line.Description, line.Quantity, line.UnitPriceSatang, line.SourceId);
                }
            }

            invoices.Add(invoice);

            webhooks.Enqueue(WebhookEventTypes.InvoiceIssued, WebhookPayloads.Invoice(invoice));

            audit.Record(new AuditEvent(
                AuditActions.InvoiceIssued,
                AuditResources.Invoice,
                invoice.Id,
                invoice.PetId,
                invoice.BranchId,
                new Dictionary<string, object?>
                {
                    ["number"] = invoice.Number,
                    ["total_satang"] = invoice.Total.Satang,
                    ["source"] = "manual",
                }));
        }, cancellationToken);

        return InvoiceDto.From(invoice!);
    }

    private static bool IsCatalogueService(string? sourceId) =>
        sourceId is { Length: > 0 } && sourceId.StartsWith(EntityId.Service + "_", StringComparison.Ordinal);

    private async Task EnsureCaseHasNoInvoiceAsync(string? consultationId, CancellationToken cancellationToken)
    {
        if (consultationId is not { Length: > 0 })
        {
            return;
        }

        var existing = await invoices.FindByConsultationAsync(consultationId, cancellationToken);

        if (existing is not null)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["consultation_id"] = [$"เคสนี้มีบิลแล้ว ({existing.Number})"],
            });
        }
    }

    private async Task<string> ResolveBranchAsync(string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is { Length: > 0 })
        {
            var branch = await catalog.FindBranchAsync(branchId, cancellationToken)
                ?? throw new NotFoundException("branch", branchId);

            return branch.Id;
        }

        var sole = await catalog.SoleActiveBranchAsync(cancellationToken);

        return sole?.Id ?? throw new ValidationFailedException(
            new Dictionary<string, string[]> { ["branch_id"] = ["คลินิกมีหลายสาขา ต้องระบุสาขา"] });
    }
}
