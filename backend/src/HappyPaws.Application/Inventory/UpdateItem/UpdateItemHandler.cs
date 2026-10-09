using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.UpdateItem;

/// <summary>
/// <c>PATCH /staff/inventory/items/{id}</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>. A field left out is not changed.
/// </summary>
/// <param name="ItemId">From the path.</param>
/// <param name="Name">The new name, which is also the new match key for typed prescriptions.</param>
/// <param name="Unit">A new label for one unit. Quantities are not converted.</param>
/// <param name="UnitPriceSatang">The selling price for dispenses from now on.</param>
/// <param name="ReorderLevel">The "ใกล้หมด" threshold.</param>
/// <param name="Active"><c>false</c> retires the item, <c>true</c> brings it back.</param>
/// <param name="IfMatchVersion">The item's <c>version</c>, when the caller sends <c>If-Match</c>.</param>
public sealed record UpdateItemCommand(
    string ItemId,
    string? Name = null,
    string? Unit = null,
    long? UnitPriceSatang = null,
    int? ReorderLevel = null,
    bool? Active = null,
    uint? IfMatchVersion = null);

public sealed class UpdateItemValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemValidator()
    {
        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("ชื่อรายการต้องไม่ว่าง")
            .MaximumLength(InventoryItem.NameMax).WithMessage($"ชื่อรายการยาวได้ไม่เกิน {InventoryItem.NameMax} ตัวอักษร")
            .When(command => command.Name is not null);

        RuleFor(command => command.Unit)
            .Must(unit => !string.IsNullOrWhiteSpace(unit)).WithMessage("หน่วยต้องไม่ว่าง")
            .MaximumLength(InventoryItem.UnitMax).WithMessage($"หน่วยยาวได้ไม่เกิน {InventoryItem.UnitMax} ตัวอักษร")
            .When(command => command.Unit is not null);

        RuleFor(command => command.UnitPriceSatang)
            .InclusiveBetween(0, InventoryItem.MaxUnitPriceSatang).WithMessage("ราคาขายต่อหน่วยไม่ถูกต้อง")
            .When(command => command.UnitPriceSatang is not null);

        RuleFor(command => command.ReorderLevel)
            .InclusiveBetween(0, InventoryItem.MaxReorderLevel).WithMessage("จุดสั่งซื้อต้องไม่ติดลบ")
            .When(command => command.ReorderLevel is not null);
    }
}

/// <summary>
/// Changes a catalogue item, or retires it.
/// <para>
/// <b>Price</b> applies to dispenses from now on: a dispense records the price it charged on the
/// prescription, and a case's medicine charges are billed from that record, so nothing already
/// dispensed is repriced. <b>Name</b> is also the match key for prescriptions typed by hand, so a
/// rename is refused with <c>409 inventory_item_exists</c> when another item holds that key, and a
/// typed prescription not yet dispensed matches the new name from now on. <b>Unit</b> is a label;
/// stock is not converted. <b>Retiring</b> takes the item out of the drug picker, refuses it on new
/// prescriptions, receipts and purchase orders, and silences its low-stock warnings, while issue,
/// transfer, count and dispense of what is left keep working and every past record still shows it.
/// </para>
/// <para>
/// The catalogue is the clinic's, like creating an item through a receipt or an order, so any
/// holder of <c>inventory:write</c> may change it. <c>If-Match</c> is honoured when sent. Audited
/// with the before and after of every field that changed; a request that changes nothing writes
/// nothing.
/// </para>
/// </summary>
public sealed class UpdateItemHandler(
    IInventoryItemRepository items,
    InventoryBranches branches,
    StockView view,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<InventoryItemDto> HandleAsync(UpdateItemCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        string? itemId = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var item = await items.FindAsync(command.ItemId, token)
                ?? throw new NotFoundException("inventory_item", command.ItemId);

            ConcurrencyGuard.Ensure(item, command.IfMatchVersion);

            var changes = new Dictionary<string, object?>();

            void Note(string field, object? before, object? after)
            {
                if (!Equals(before, after))
                {
                    changes[field] = new Dictionary<string, object?> { ["before"] = before, ["after"] = after };
                }
            }

            if (command.Name is { } name && InventoryItem.KeyOf(name) is var key && key != item.NameKey)
            {
                if (await items.FindByNameKeyAsync(key, token) is { } holder && holder.Id != item.Id)
                {
                    throw InventoryItemResolver.Exists(holder.Id, holder.IsRetired);
                }
            }

            var was = (item.Name, item.Unit, item.UnitPriceSatang, item.ReorderLevel, Active: !item.IsRetired);

            if (command.Name is { } newName)
            {
                item.Rename(newName);
            }

            if (command.Unit is { } unit)
            {
                item.ChangeUnit(unit);
            }

            if (command.UnitPriceSatang is { } price)
            {
                item.Reprice(price);
            }

            if (command.ReorderLevel is { } level)
            {
                item.ChangeReorderLevel(level);
            }

            if (command.Active is false)
            {
                item.Retire(clock.UtcNow);
            }
            else if (command.Active is true)
            {
                item.Reactivate();
            }

            Note("name", was.Name, item.Name);
            Note("unit", was.Unit, item.Unit);
            Note("unit_price_satang", was.UnitPriceSatang, item.UnitPriceSatang);
            Note("reorder_level", was.ReorderLevel, item.ReorderLevel);
            Note("active", was.Active, !item.IsRetired);

            if (changes.Count > 0)
            {
                audit.Record(new AuditEvent(
                    AuditActions.InventoryItemUpdated,
                    AuditResources.InventoryItem,
                    item.Id,
                    Details: new Dictionary<string, object?>
                    {
                        ["name"] = item.Name,
                        ["kind"] = WireEnum.Of(item.Kind),
                        ["changes"] = changes,
                    }));
            }

            itemId = item.Id;
        }, cancellationToken);

        var scope = await branches.ForReadAsync(null, cancellationToken);

        return await view.ForAsync(itemId!, scope, cancellationToken);
    }
}
