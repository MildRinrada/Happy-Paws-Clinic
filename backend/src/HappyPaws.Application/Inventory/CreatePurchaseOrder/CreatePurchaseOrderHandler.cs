using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.CreatePurchaseOrder;

/// <summary>
/// <c>POST /staff/inventory/purchase-orders</c> — "สั่งซื้อจากซัพพลายเออร์ · รับของบางส่วนได้".
/// </summary>
/// <param name="BranchId">Where the goods are for. The caller's only branch when absent.</param>
/// <param name="SupplierName">Who the order goes to.</param>
/// <param name="ExpectedOn">When the supplier says it will arrive.</param>
/// <param name="Note">Anything the order should carry: terms, a contact, a quote number.</param>
/// <param name="Lines">One to fifty items, each once.</param>
public sealed record CreatePurchaseOrderCommand(
    string? BranchId = null,
    string? SupplierName = null,
    DateOnly? ExpectedOn = null,
    string? Note = null,
    IReadOnlyList<PurchaseOrderLineInput>? Lines = null);

/// <param name="ItemId">An item already in the catalogue.</param>
/// <param name="NewItem">Or a new one, created with the order.</param>
/// <param name="Quantity">Units ordered.</param>
/// <param name="UnitCostSatang">What one unit costs from this supplier.</param>
public sealed record PurchaseOrderLineInput(
    string? ItemId = null,
    NewItemInput? NewItem = null,
    int? Quantity = null,
    long? UnitCostSatang = null);

public sealed class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderValidator()
    {
        RuleFor(command => command.BranchId).MaximumLength(40);

        RuleFor(command => command.SupplierName)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("ต้องระบุชื่อซัพพลายเออร์")
            .MaximumLength(PurchaseOrder.SupplierMax);

        RuleFor(command => command.Note).MaximumLength(PurchaseOrder.NoteMax);

        RuleFor(command => command.Lines)
            .NotEmpty().WithMessage("ใบสั่งซื้อต้องมีอย่างน้อยหนึ่งรายการ")
            .Must(lines => lines!.Count <= PurchaseOrder.MaxLines).WithMessage($"ใบสั่งซื้อหนึ่งใบมีได้ไม่เกิน {PurchaseOrder.MaxLines} รายการ")
            .Must(NoRepeats).WithMessage("รายการเดียวกันอยู่ในใบสั่งซื้อซ้ำ")
            .When(command => command.Lines is not null, ApplyConditionTo.CurrentValidator);

        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(input => input.ItemId)
                .Must((input, itemId) => string.IsNullOrWhiteSpace(itemId) != (input.NewItem is null))
                .WithMessage("ต้องระบุ item_id หรือ new_item อย่างใดอย่างหนึ่ง")
                .MaximumLength(40);

            line.RuleFor(input => input.NewItem!).SetValidator(new NewItemValidator()).When(input => input.NewItem is not null);

            line.RuleFor(input => input.Quantity)
                .NotNull().WithMessage("ต้องระบุจำนวน")
                .InclusiveBetween(1, PurchaseOrderLine.MaxQuantity).WithMessage("จำนวนสั่งซื้อต้องมากกว่าศูนย์");

            line.RuleFor(input => input.UnitCostSatang)
                .NotNull().WithMessage("ต้องระบุต้นทุนต่อหน่วย")
                .InclusiveBetween(0, InventoryItem.MaxUnitPriceSatang).WithMessage("ต้นทุนต่อหน่วยไม่ถูกต้อง");
        });
    }

    private static bool NoRepeats(IReadOnlyList<PurchaseOrderLineInput>? lines)
    {
        var keys = lines!
            .Select(line => line.ItemId is { Length: > 0 } id
                ? $"id:{id.Trim()}"
                : $"name:{InventoryItem.KeyOf(line.NewItem?.Name ?? string.Empty)}")
            .ToList();

        return keys.Distinct(StringComparer.Ordinal).Count() == keys.Count;
    }
}

/// <summary>
/// Raises an order to a supplier.
/// <para>
/// Changes no stock: goods arrive later, as receipts against the order through
/// <c>POST /staff/inventory/movements</c>, as many as it takes. A line may introduce a drug the
/// clinic has never stocked, which is created in the catalogue with the order, because ordering it
/// is the first moment the clinic knows about it.
/// </para>
/// <para>
/// Numbered <c>PO-YYYYMM-NNNN</c> in the clinic's calendar, inside the transaction, like an invoice.
/// Audited, as money the clinic has committed to spend.
/// </para>
/// </summary>
public sealed class CreatePurchaseOrderHandler(
    InventoryItemResolver resolver,
    IPurchaseOrderRepository orders,
    InventoryBranches branches,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<PurchaseOrderDto> HandleAsync(CreatePurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var branchId = await branches.ForWriteAsync(command.BranchId, "branch_id", cancellationToken);

        PurchaseOrder? order = null;
        var catalogue = new Dictionary<string, InventoryItem>(StringComparer.Ordinal);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            catalogue.Clear();
            var now = clock.UtcNow;

            var raised = PurchaseOrder.Raise(
                await orders.NextNumberAsync(now, token),
                branchId,
                command.SupplierName!,
                staffId,
                now,
                command.ExpectedOn,
                command.Note);

            foreach (var line in command.Lines!)
            {
                var item = await resolver.ResolveAsync(line.ItemId, line.NewItem, staffId, token);
                catalogue[item.Id] = item;
                raised.AddLine(item.Id, line.Quantity!.Value, line.UnitCostSatang!.Value);
            }

            orders.Add(raised);

            audit.Record(new AuditEvent(
                AuditActions.PurchaseOrderRaised,
                AuditResources.PurchaseOrder,
                raised.Id,
                BranchId: branchId,
                Details: new Dictionary<string, object?>
                {
                    ["number"] = raised.Number,
                    ["supplier_name"] = raised.SupplierName,
                    ["lines"] = raised.Lines.Count,
                    ["total_cost_satang"] = raised.TotalCost.Satang,
                }));

            order = raised;
        }, cancellationToken);

        return PurchaseOrderDto.From(order!, catalogue);
    }
}
