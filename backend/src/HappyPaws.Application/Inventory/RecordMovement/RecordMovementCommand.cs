using FluentValidation;
using HappyPaws.Application.Common;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.RecordMovement;

/// <summary>
/// <c>POST /staff/inventory/movements</c> — "รับเข้า จ่ายออก โอนสาขา · อ้างอิงเอกสารต้นทาง".
/// </summary>
/// <param name="Type"><c>receive</c>, <c>issue</c> or <c>transfer</c>.</param>
/// <param name="BranchId">Where goods arrive (receive) or leave from (issue, transfer).</param>
/// <param name="ToBranchId">Transfer only: where they go.</param>
/// <param name="ItemId">The item. A receipt may instead describe a new one in <paramref name="NewItem"/>.</param>
/// <param name="NewItem">Receive only: a drug or supply the catalogue does not have yet, created with the receipt.</param>
/// <param name="Quantity">Units, at least one.</param>
/// <param name="LotId">Issue and transfer: take from this lot. First expiry first when absent.</param>
/// <param name="LotNumber">Receive: the supplier's lot number.</param>
/// <param name="ExpiresOn">Receive: the lot's expiry date.</param>
/// <param name="UnitCostSatang">Receive: what one unit cost. The order line's cost when receiving against one.</param>
/// <param name="PurchaseOrderId">Receive: the order these goods arrived against — "รับของบางส่วนได้".</param>
/// <param name="Reference">"อ้างอิงเอกสารต้นทาง": the delivery note or form number.</param>
/// <param name="Note">Why, in words.</param>
public sealed record RecordMovementCommand(
    string? Type = null,
    string? BranchId = null,
    string? ToBranchId = null,
    string? ItemId = null,
    NewItemInput? NewItem = null,
    int? Quantity = null,
    string? LotId = null,
    string? LotNumber = null,
    DateOnly? ExpiresOn = null,
    long? UnitCostSatang = null,
    string? PurchaseOrderId = null,
    string? Reference = null,
    string? Note = null)
{
    public const int MaxQuantity = 1_000_000;

    public MovementType ParsedType => WireEnum.TryParse<MovementType>(Type, out var type) ? type : default;
}

public sealed class RecordMovementValidator : AbstractValidator<RecordMovementCommand>
{
    private const string ReceiveOnly = "ใช้ได้กับการรับเข้าเท่านั้น";

    public RecordMovementValidator()
    {
        RuleFor(command => command.Type)
            .Must(type => WireEnum.TryParse<MovementType>(type, out _))
            .WithMessage($"type ต้องเป็น {WireEnum.Names<MovementType>()}");

        RuleFor(command => command.Quantity)
            .NotNull().WithMessage("ต้องระบุจำนวน")
            .InclusiveBetween(1, RecordMovementCommand.MaxQuantity).WithMessage("จำนวนต้องมากกว่าศูนย์");

        RuleFor(command => command.BranchId).MaximumLength(40);
        RuleFor(command => command.ItemId).MaximumLength(40);
        RuleFor(command => command.LotId).MaximumLength(40);
        RuleFor(command => command.PurchaseOrderId).MaximumLength(40);
        RuleFor(command => command.LotNumber).MaximumLength(StockLot.LotNumberMax);
        RuleFor(command => command.Reference).MaximumLength(StockMovement.ReferenceMax);
        RuleFor(command => command.Note).MaximumLength(StockMovement.NoteMax);

        When(command => WireEnum.TryParse<MovementType>(command.Type, out var type) && type == MovementType.Receive, () =>
        {
            RuleFor(command => command.ItemId)
                .Must((command, itemId) => string.IsNullOrWhiteSpace(itemId) != (command.NewItem is null))
                .WithMessage("ต้องระบุ item_id หรือ new_item อย่างใดอย่างหนึ่ง");

            RuleFor(command => command.NewItem!).SetValidator(new NewItemValidator()).When(command => command.NewItem is not null);

            RuleFor(command => command.UnitCostSatang)
                .InclusiveBetween(0, InventoryItem.MaxUnitPriceSatang).WithMessage("ต้นทุนต่อหน่วยไม่ถูกต้อง")
                .When(command => command.UnitCostSatang is not null);

            RuleFor(command => command.LotId).Empty().WithMessage("การรับเข้าไม่ใช้ lot_id ระบุ lot_number และ expires_on แทน");
            RuleFor(command => command.ToBranchId).Empty().WithMessage("ใช้ได้กับการโอนสาขาเท่านั้น");
        });

        When(command => WireEnum.TryParse<MovementType>(command.Type, out var type) && type != MovementType.Receive, () =>
        {
            RuleFor(command => command.ItemId).NotEmpty().WithMessage("ต้องระบุ item_id");
            RuleFor(command => command.NewItem).Null().WithMessage(ReceiveOnly);
            RuleFor(command => command.LotNumber).Empty().WithMessage(ReceiveOnly);
            RuleFor(command => command.ExpiresOn).Null().WithMessage(ReceiveOnly);
            RuleFor(command => command.UnitCostSatang).Null().WithMessage(ReceiveOnly);
            RuleFor(command => command.PurchaseOrderId).Empty().WithMessage(ReceiveOnly);
        });

        When(command => WireEnum.TryParse<MovementType>(command.Type, out var type) && type == MovementType.Issue, () =>
            RuleFor(command => command.ToBranchId).Empty().WithMessage("ใช้ได้กับการโอนสาขาเท่านั้น"));

        When(command => WireEnum.TryParse<MovementType>(command.Type, out var type) && type == MovementType.Transfer, () =>
        {
            RuleFor(command => command.ToBranchId)
                .NotEmpty().WithMessage("ต้องระบุสาขาปลายทาง")
                .MaximumLength(40)
                .Must((command, to) => !string.Equals(to?.Trim(), command.BranchId?.Trim(), StringComparison.Ordinal))
                .WithMessage("สาขาปลายทางต้องไม่ใช่สาขาเดียวกับต้นทาง");
        });
    }
}
