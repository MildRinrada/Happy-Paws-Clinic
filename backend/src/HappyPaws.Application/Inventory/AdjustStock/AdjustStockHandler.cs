using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.AdjustStock;

/// <summary>
/// <c>POST /staff/inventory/adjustments</c> — "ปรับยอดหลังนับสต๊อก · ต้องระบุเหตุผล".
/// </summary>
/// <param name="BranchId">The branch that was counted. The caller's only branch when absent.</param>
/// <param name="ItemId">The item that was counted.</param>
/// <param name="LotId">The lot that was counted. Needed only when the branch holds more than one lot of the item.</param>
/// <param name="CountedQuantity">What is physically on the shelf. Zero is a count too.</param>
/// <param name="Reason">Required: why the books were wrong, or that the count is routine.</param>
public sealed record AdjustStockCommand(
    string? BranchId = null,
    string? ItemId = null,
    string? LotId = null,
    int? CountedQuantity = null,
    string? Reason = null);

public sealed class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithMessage("ต้องระบุ item_id").MaximumLength(40);
        RuleFor(command => command.BranchId).MaximumLength(40);
        RuleFor(command => command.LotId).MaximumLength(40);

        RuleFor(command => command.CountedQuantity)
            .NotNull().WithMessage("ต้องระบุจำนวนที่นับได้")
            .InclusiveBetween(0, 1_000_000).WithMessage("จำนวนที่นับได้ต้องไม่ติดลบ");

        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithMessage("ต้องระบุเหตุผลในการปรับยอด")
            .MaximumLength(StockMovement.NoteMax).WithMessage($"เหตุผลยาวได้ไม่เกิน {StockMovement.NoteMax} ตัวอักษร");
    }
}

/// <summary>
/// Sets the books to a count.
/// <para>
/// <b>Counted, not "plus or minus".</b> The person at the shelf knows what they counted, not the
/// difference; the difference is worked out here, under the stock lock, against the balance at that
/// moment, and written to the ledger as one signed row. A count that matches the books still writes
/// a row of zero, which is the record that the count happened.
/// </para>
/// <para>
/// <b>Which lot.</b> A count is of a lot. When the branch holds units in exactly one lot, that lot is
/// meant; when it holds none, a count of stock found on the shelf opens a lot without a number; when
/// it holds several, <c>lot_id</c> is required.
/// </para>
/// <para>
/// Audited, with the balance before and after and the reason, because an adjustment is the one stock
/// change with no document behind it.
/// </para>
/// </summary>
public sealed class AdjustStockHandler(
    IInventoryItemRepository items,
    IStockLedger ledger,
    InventoryBranches branches,
    StockView view,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<StockChangeDto> HandleAsync(AdjustStockCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var branchId = await branches.ForWriteAsync(command.BranchId, "branch_id", cancellationToken);
        var itemId = command.ItemId!.Trim();

        StockMovement? written = null;
        StockLot? counted = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;

            if (await items.FindAsync(itemId, token) is null)
            {
                throw new NotFoundException("inventory_item", itemId);
            }

            var key = new StockKey(itemId, branchId);
            await ledger.LockAsync([key], token);

            var lots = await ledger.LotsForUpdateAsync(key, token);
            var lot = Pick(lots, command.LotId, itemId, branchId, now);
            var before = lot.QuantityOnHand;

            var movement = StockMovement.Adjust(lot, command.CountedQuantity!.Value, command.Reason!, staffId, now);
            ledger.Append(movement);

            audit.Record(new AuditEvent(
                AuditActions.InventoryAdjusted,
                AuditResources.StockLot,
                lot.Id,
                BranchId: branchId,
                Details: new Dictionary<string, object?>
                {
                    ["item_id"] = itemId,
                    ["lot_number"] = lot.LotNumber,
                    ["before"] = before,
                    ["counted"] = lot.QuantityOnHand,
                    ["difference"] = movement.Quantity,
                    ["reason"] = movement.Note,
                    ["movement_id"] = movement.Id,
                }));

            (written, counted) = (movement, lot);
        }, cancellationToken);

        var stock = await view.ForAsync(itemId, [branchId], cancellationToken);

        return new StockChangeDto(stock, [StockMovementDto.From(written!, counted!)]);
    }

    private StockLot Pick(IReadOnlyList<StockLot> lots, string? lotId, string itemId, string branchId, DateTimeOffset now)
    {
        if (lotId is { Length: > 0 })
        {
            return lots.FirstOrDefault(lot => lot.Id == lotId.Trim())
                ?? throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["lot_id"] = ["ไม่พบล็อตนี้ของรายการนี้ในสาขา"],
                });
        }

        var holding = lots.Where(lot => lot.QuantityOnHand > 0).ToList();

        switch (holding.Count)
        {
            case 1:
                return holding[0];
            case > 1:
                throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["lot_id"] = ["สาขานี้มีรายการนี้หลายล็อต ต้องระบุล็อตที่นับ"],
                });
        }

        // Nothing on the books: units found on the shelf go into a lot with no number, reused if one
        // already exists so repeated counts do not pile up empty lots.
        var unnumbered = lots.FirstOrDefault(lot => lot.Matches(null, null));

        if (unnumbered is not null)
        {
            return unnumbered;
        }

        var opened = StockLot.Open(itemId, branchId, lotNumber: null, expiresOn: null, now);
        ledger.AddLot(opened);

        return opened;
    }
}
