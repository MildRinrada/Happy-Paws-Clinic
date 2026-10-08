namespace HappyPaws.Application.Prescriptions.Dtos;

/// <summary>
/// <c>GET /staff/prescriptions/drug-options</c> — medicines a prescriber can pick, with the stock of
/// the one branch named in <c>branch_id</c>. Not in the API reference
/// (<c>docs/contract-extensions.md § 20</c>).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>branch_id</c>: The branch the stock figures are for: the one asked for, or the one chosen when none was.</item>
/// <item><c>data</c>: Medicines by name, at most <c>limit</c> of them.</item>
/// </list>
/// </remarks>
public sealed record DrugOptionsDto(string BranchId, IReadOnlyList<DrugOptionDto> Data);

/// <summary>One medicine in the picker. No cost, no lots, no suppliers, no other branch.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>item_id</c>: Send back as <c>inventory_item_id</c> when writing the prescription.</item>
/// <item><c>unit</c>: What the prescription's quantity counts: <c>เม็ด</c>, <c>ขวด</c>.</item>
/// <item><c>unit_price_satang</c>: The selling price of one unit, charged when dispensed.</item>
/// <item><c>available</c>: Usable units at the branch: lots not yet expired.</item>
/// <item><c>status</c>: <c>ok</c>, <c>low</c> or <c>out</c> at the branch, judged like the stock list.</item>
/// </list>
/// </remarks>
public sealed record DrugOptionDto(
    string ItemId,
    string Name,
    string Unit,
    long UnitPriceSatang,
    int Available,
    string Status);
