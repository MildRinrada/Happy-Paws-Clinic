using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Customers.Dtos;

namespace HappyPaws.Application.Customers.SearchCustomers;

/// <summary>
/// <c>GET /staff/customers?q=</c> — "ค้นด้วยชื่อ เบอร์ HN หรือชื่อสัตว์".
/// <para>
/// One box, four things behind it. What a counter is actually told is "คุณสมชาย", or a phone
/// number, or "ลูน่า", or an HN read off a vaccination book — and the person saying it does not
/// know which of those the system files it under.
/// </para>
/// </summary>
public sealed record SearchCustomersQuery(
    string? Q = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// Searches the customer register.
/// <para>
/// No branch filter, and that is not an oversight. A customer belongs to the clinic, not to a
/// branch — the contract puts <c>branches</c> on staff and on appointments, never on a person — so
/// narrowing this by the caller's branch would hide a customer from the site they walked into.
/// </para>
/// </summary>
public sealed class SearchCustomersHandler(ICustomerRepository customers)
{
    public async Task<CursorPage<CustomerDto>> HandleAsync(
        SearchCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await customers.SearchAsync(
            query.Q, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        return new CursorPage<CustomerDto>([.. page.Data.Select(CustomerDto.From)], page.NextCursor);
    }
}
