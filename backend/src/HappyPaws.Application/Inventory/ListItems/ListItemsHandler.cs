using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Inventory.Dtos;
using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory.ListItems;

/// <summary><c>GET /staff/inventory/items?q=&amp;branch=&amp;status=&amp;active=</c>.</summary>
/// <param name="Search">Part of the item's name.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
/// <param name="Status"><c>ok</c>, <c>low</c> or <c>out</c>: items with at least one listed branch in that state.</param>
/// <param name="Active">
/// <c>true</c> for items in use, <c>false</c> for retired ones, absent for both
/// (<c>docs/contract-extensions.md § 25</c>, not in the API reference).
/// </param>
public sealed record ListItemsQuery(string? Search = null, string? BranchId = null, string? Status = null, bool? Active = null);

/// <summary>
/// "ยาและเวชภัณฑ์ · คงเหลือรายสาขา" — the catalogue with each branch's stock.
/// <para>
/// The whole catalogue in one answer, by name, like the service list: a clinic stocks hundreds of
/// items, not hundreds of thousands, and the screen filters and searches it in place. Every item is
/// listed against every branch in scope, at zero when the branch holds none, so "we have never
/// stocked this here" and "we have run out" read the same way: <c>out</c>.
/// </para>
/// </summary>
public sealed class ListItemsHandler(IInventoryItemRepository items, InventoryBranches branches, StockView view)
{
    public async Task<IReadOnlyList<InventoryItemDto>> HandleAsync(ListItemsQuery query, CancellationToken cancellationToken = default)
    {
        StockStatus? status = null;

        if (query.Status is { Length: > 0 } wanted)
        {
            status = WireEnum.TryParse<StockStatus>(wanted, out var parsed)
                ? parsed
                : throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["status"] = [$"status ต้องเป็น {WireEnum.Names<StockStatus>()}"],
                });
        }

        var scope = await branches.ForReadAsync(query.BranchId, cancellationToken);
        var catalogue = await items.ListAsync(query.Search?.Trim(), query.Active is { } active ? !active : null, cancellationToken);
        var stock = await view.BuildAsync(catalogue, scope, cancellationToken);

        if (status is not { } filter)
        {
            return stock;
        }

        var wire = WireEnum.Of(filter);
        return [.. stock.Where(item => item.Branches.Any(branch => branch.Status == wire))];
    }
}
