using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.Application.Referrals.Staff;

/// <summary><c>GET /staff/referrals?customer=&amp;limit=&amp;cursor=</c>.</summary>
/// <param name="CustomerId">Only referrals where this customer is either side.</param>
/// <param name="Limit">Page size, 1 to 100.</param>
/// <param name="Cursor">From the previous page.</param>
public sealed record ListReferralsQuery(string? CustomerId = null, int Limit = CursorPaging.DefaultLimit, string? Cursor = null);

/// <summary>
/// Who referred whom (<c>crm:read</c>), newest first: the status, the reason a rejected one earned
/// nothing, the points each side got, and when. Not in the API reference:
/// <c>docs/contract-extensions.md § 39</c>. Clinic-wide like the points ledger, because a customer
/// and their points belong to the clinic, not to a branch.
/// </summary>
public sealed class ListReferralsHandler(IReferralRepository referrals)
{
    public async Task<CursorPage<StaffReferralDto>> HandleAsync(ListReferralsQuery query, CancellationToken cancellationToken = default)
    {
        var customer = query.CustomerId?.Trim();

        if (customer is { Length: > 40 })
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["customer"] = ["customer ยาวเกินไป"],
            });
        }

        var page = await referrals.ListAsync(
            string.IsNullOrEmpty(customer) ? null : customer,
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        return new CursorPage<StaffReferralDto>([.. page.Data.Select(StaffReferralDto.From)], page.NextCursor);
    }
}
