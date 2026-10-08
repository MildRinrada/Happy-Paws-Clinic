using FluentValidation;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Inventory;
using HappyPaws.Application.Prescriptions.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Prescriptions.ListDrugOptions;

/// <summary><c>GET /staff/prescriptions/drug-options?q=&amp;branch=&amp;limit=</c>.</summary>
/// <param name="Search">Part of the drug's name.</param>
/// <param name="BranchId">The branch whose stock is shown, normally the case's. See <see cref="InventoryBranches.ForWriteAsync"/>.</param>
/// <param name="Limit">1 to <see cref="ListDrugOptionsHandler.MaxLimit"/>; <see cref="ListDrugOptionsHandler.DefaultLimit"/> when absent.</param>
public sealed record ListDrugOptionsQuery(string? Search = null, string? BranchId = null, int? Limit = null);

public sealed class ListDrugOptionsValidator : AbstractValidator<ListDrugOptionsQuery>
{
    public ListDrugOptionsValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(InventoryItem.NameMax).WithMessage($"คำค้นยาวได้ไม่เกิน {InventoryItem.NameMax} ตัวอักษร")
            .OverridePropertyName("q");

        RuleFor(query => query.Limit)
            .InclusiveBetween(1, ListDrugOptionsHandler.MaxLimit)
            .WithMessage($"limit ต้องอยู่ระหว่าง 1 ถึง {ListDrugOptionsHandler.MaxLimit}")
            .When(query => query.Limit is not null);
    }
}

/// <summary>
/// The prescriber's drug picker. Not in the API reference (<c>docs/contract-extensions.md § 20</c>).
/// <para>
/// <b>Why not <c>GET /staff/inventory/items</c>.</b> That list is the stock room's: every lot, open
/// purchase orders with supplier names, both kinds of item, every branch. It needs
/// <c>inventory:read</c>, which the contract's <c>veterinarian</c> role does not hold. Widening it
/// to <c>rx:write</c> would hand prescribers the purchasing side as well. This read answers only
/// what writing a drug needs, under the scope that writes one.
/// </para>
/// <para>
/// <b>What a row carries.</b> The item's id (sent back as <c>inventory_item_id</c>), its name and
/// unit, the selling price per unit (what the owner will be charged), and the usable units and
/// <c>ok</c>/<c>low</c>/<c>out</c> status at one branch. Medicines only: a supply is never
/// prescribed. Nothing about cost, lots, suppliers or other branches.
/// </para>
/// <para>
/// <b>One branch.</b> Stock is a branch's, and a drug is dispensed at the case's branch, so the
/// picker shows that branch: the one asked for (open, and one the caller works at, or <c>403</c>),
/// else the caller's only branch, else the clinic's only open one; otherwise <c>422</c>.
/// </para>
/// <para>
/// Items out of stock are listed with <c>out</c>, not hidden. The vet may still write the drug (it
/// may be on its way, or given from another shelf); the dispense will then report the shortfall.
/// </para>
/// </summary>
public sealed class ListDrugOptionsHandler(
    RequestValidator validator,
    IInventoryItemRepository items,
    IStockLedger ledger,
    InventoryBranches branches,
    ClinicDays clinicDays)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    public async Task<DrugOptionsDto> HandleAsync(ListDrugOptionsQuery query, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(query, cancellationToken);

        var branchId = await branches.ForWriteAsync(query.BranchId, "branch", cancellationToken);
        var medicines = await items.SearchMedicinesAsync(query.Search?.Trim(), query.Limit ?? DefaultLimit, cancellationToken);

        if (medicines.Count == 0)
        {
            return new DrugOptionsDto(branchId, []);
        }

        var lots = await ledger.LotsAsync([.. medicines.Select(item => item.Id)], [branchId], cancellationToken);
        var lotsByItem = lots.ToLookup(lot => lot.ItemId);
        var today = clinicDays.Today;

        return new DrugOptionsDto(
            branchId,
            [
                .. medicines.Select(item =>
                {
                    var available = StockAllocation.Available(lotsByItem[item.Id], today);

                    return new DrugOptionDto(
                        item.Id,
                        item.Name,
                        item.Unit,
                        item.UnitPriceSatang,
                        available,
                        WireEnum.Of(StockLevels.StatusOf(available, item.ReorderLevel)));
                }),
            ]);
    }
}
