using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory;

/// <summary>
/// A new catalogue entry, written on a purchase-order line or a receipt. The contract has no
/// "create item" endpoint, so an item is born the first time the clinic orders or receives it.
/// </summary>
/// <param name="Name">As it will be matched against prescriptions — exactly, after spacing and case.</param>
/// <param name="Kind"><c>medicine</c> (default) or <c>supply</c>.</param>
/// <param name="Unit">What one unit is called: <c>เม็ด</c>, <c>ขวด</c>.</param>
/// <param name="UnitPriceSatang">The selling price of one unit, charged at dispense. Zero gives it away.</param>
/// <param name="ReorderLevel">"ใกล้หมด" threshold. Zero when absent.</param>
public sealed record NewItemInput(
    string? Name = null,
    string? Kind = null,
    string? Unit = null,
    long? UnitPriceSatang = null,
    int? ReorderLevel = null);

public sealed class NewItemValidator : AbstractValidator<NewItemInput>
{
    public NewItemValidator()
    {
        RuleFor(item => item.Name)
            .NotEmpty().WithMessage("ต้องระบุชื่อรายการ")
            .MaximumLength(InventoryItem.NameMax).WithMessage($"ชื่อรายการยาวได้ไม่เกิน {InventoryItem.NameMax} ตัวอักษร");

        RuleFor(item => item.Kind)
            .Must(kind => kind is null || WireEnum.TryParse<InventoryItemKind>(kind, out _))
            .WithMessage($"kind ต้องเป็น {WireEnum.Names<InventoryItemKind>()}");

        RuleFor(item => item.Unit)
            .NotEmpty().WithMessage("ต้องระบุหน่วย")
            .MaximumLength(InventoryItem.UnitMax).WithMessage($"หน่วยยาวได้ไม่เกิน {InventoryItem.UnitMax} ตัวอักษร");

        RuleFor(item => item.UnitPriceSatang)
            .NotNull().WithMessage("ต้องระบุราคาขายต่อหน่วย")
            .InclusiveBetween(0, InventoryItem.MaxUnitPriceSatang).WithMessage("ราคาขายต่อหน่วยไม่ถูกต้อง");

        RuleFor(item => item.ReorderLevel)
            .InclusiveBetween(0, InventoryItem.MaxReorderLevel).WithMessage("จุดสั่งซื้อต้องไม่ติดลบ")
            .When(item => item.ReorderLevel is not null);
    }
}

/// <summary>
/// Turns "this item" or "a new item called …" into a catalogue entry, creating it when needed.
/// <para>
/// A new item whose name is already in the catalogue is refused with <c>409 inventory_item_exists</c>
/// and the existing id, rather than silently used: the request carried a price and a unit, and
/// quietly ignoring them would leave the caller believing they were stored.
/// </para>
/// </summary>
public sealed class InventoryItemResolver(IInventoryItemRepository items, IAuditTrail audit)
{
    /// <summary>Call inside the transaction that will use the item.</summary>
    public async Task<InventoryItem> ResolveAsync(
        string? itemId,
        NewItemInput? newItem,
        string staffId,
        CancellationToken cancellationToken = default)
    {
        if (itemId is { Length: > 0 })
        {
            return await items.FindAsync(itemId.Trim(), cancellationToken)
                ?? throw new NotFoundException("inventory_item", itemId);
        }

        var input = newItem ?? throw new InvalidOperationException("validator lets neither item_id nor new_item through");

        if (await items.FindByNameKeyAsync(InventoryItem.KeyOf(input.Name!), cancellationToken) is { } existing)
        {
            throw Exists(existing.Id);
        }

        WireEnum.TryParse<InventoryItemKind>(input.Kind, out var kind);

        var item = InventoryItem.Create(
            input.Name!,
            input.Kind is null ? InventoryItemKind.Medicine : kind,
            input.Unit!,
            input.UnitPriceSatang ?? 0,
            input.ReorderLevel ?? 0,
            staffId);

        items.Add(item);

        audit.Record(new AuditEvent(
            AuditActions.InventoryItemCreated,
            AuditResources.InventoryItem,
            item.Id,
            Details: new Dictionary<string, object?>
            {
                ["name"] = item.Name,
                ["kind"] = WireEnum.Of(item.Kind),
                ["unit"] = item.Unit,
                ["unit_price_satang"] = item.UnitPriceSatang,
                ["reorder_level"] = item.ReorderLevel,
            }));

        return item;
    }

    /// <summary>The refusal for a name already taken. Also what a lost race on the unique index becomes.</summary>
    public static ConflictException Exists(string? existingId) =>
        new(ErrorCodes.InventoryItemExists, "มีรายการชื่อนี้ในคลังแล้ว ใช้ item_id ของรายการเดิม")
        {
            MessageEn = "An item with this name already exists.",
            Details = existingId is null ? null : new Dictionary<string, object> { ["item_id"] = existingId },
        };
}
