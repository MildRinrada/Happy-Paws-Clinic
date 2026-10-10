using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Inventory;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Forecasting.Inventory;

/// <summary><c>GET /staff/inventory/forecast?branch=&amp;cover_days=</c>.</summary>
/// <param name="BranchId">One branch; the caller's only branch, or the clinic's only one, when absent.</param>
/// <param name="CoverDays">1 to 180; 30 when absent.</param>
public sealed record GetInventoryForecastQuery(string? BranchId = null, int? CoverDays = null);

/// <summary>
/// "ยาจะหมดเมื่อไรและควรสั่งเท่าไร" from the branch's own ledger, by
/// <see cref="InventoryForecastRules"/>. Stock comes from <see cref="StockView"/>, the same builder
/// as the item list, so <c>available</c> and <c>on_order</c> here are the numbers on that page.
/// Retired items are left out: nobody reorders them. Not in the API reference:
/// <c>docs/contract-extensions.md § 33</c>.
/// </summary>
public sealed class GetInventoryForecastHandler(
    IInventoryItemRepository items,
    StockView stock,
    IForecastQueries queries,
    ForecastBranches branches,
    ClinicDays clinicDays)
{
    public async Task<InventoryForecastDto> HandleAsync(GetInventoryForecastQuery query, CancellationToken cancellationToken = default)
    {
        var coverDays = query.CoverDays ?? InventoryForecastRules.DefaultCoverDays;

        if (coverDays is < 1 or > InventoryForecastRules.MaxCoverDays)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["cover_days"] = [$"cover_days ต้องอยู่ระหว่าง 1 ถึง {InventoryForecastRules.MaxCoverDays}"],
            });
        }

        var branchId = await branches.OneAsync(query.BranchId, cancellationToken);
        var today = clinicDays.Today;

        var catalogue = await items.ListAsync(search: null, retired: false, cancellationToken);
        var levels = await stock.BuildAsync(catalogue, [branchId], cancellationToken);
        var usage = (await queries.StockUsageAsync(
                branchId,
                clinicDays.StartOf(today.AddDays(-InventoryForecastRules.LongWindowDays)),
                clinicDays.StartOf(today.AddDays(-InventoryForecastRules.ShortWindowDays)),
                clinicDays.StartOf(today),
                cancellationToken))
            .ToDictionary(row => row.ItemId, StringComparer.Ordinal);

        var rows = levels.Select(item =>
        {
            var branch = item.Branches.Single();
            usage.TryGetValue(item.Id, out var used);

            var history = InventoryForecastRules.HistoryDays(
                used is null ? null : clinicDays.DateOf(used.FirstMovementAt),
                today);
            var shortAverage = InventoryForecastRules.DailyAverage(used?.UsedShort ?? 0, InventoryForecastRules.ShortWindowDays, history);
            var longAverage = InventoryForecastRules.DailyAverage(used?.UsedLong ?? 0, InventoryForecastRules.LongWindowDays, history);
            var rate = InventoryForecastRules.DailyRate(shortAverage, longAverage);
            var daysUntilOut = InventoryForecastRules.DaysUntilOut(branch.Available, rate);

            return new InventoryForecastItemDto(
                item.Id,
                item.Name,
                item.Kind,
                item.Unit,
                item.ReorderLevel,
                branch.Available,
                branch.OnOrder,
                used?.UsedShort ?? 0,
                used?.UsedLong ?? 0,
                InventoryForecastRules.Round(shortAverage),
                InventoryForecastRules.Round(longAverage),
                InventoryForecastRules.Round(rate),
                daysUntilOut,
                daysUntilOut is { } days ? today.AddDays(days) : null,
                InventoryForecastRules.SuggestedOrder(rate, branch.Available, branch.OnOrder, coverDays),
                history,
                WireEnum.Of(InventoryForecastRules.ConfidenceOf(history)));
        });

        return new InventoryForecastDto(
            branchId,
            today,
            coverDays,
            InventoryForecastRules.LeadTimeDays,
            [.. rows
                .OrderBy(row => row.DaysUntilOut is null)
                .ThenBy(row => row.DaysUntilOut)
                .ThenBy(row => row.Name, StringComparer.Ordinal)]);
    }
}
