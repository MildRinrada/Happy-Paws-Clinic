using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers.GetConsents;

/// <summary>
/// <c>GET /portal/consents</c> and <c>GET /staff/customers/{id}/consents</c>.
/// <para>
/// One handler for both. Who the customer is has already been decided by the time it runs — the
/// portal route resolves it from the token, the staff route from the path behind
/// <c>customer:read</c> — and the answer to "what has this person agreed to" cannot differ by who
/// is asking.
/// </para>
/// </summary>
public sealed record GetConsentsQuery(string CustomerId, bool IncludeHistory = true);

/// <summary>
/// Reports the current position on every purpose, and how it was reached.
/// <para>
/// Purposes never recorded are returned with their default and <c>is_default: true</c> rather than
/// omitted. A portal screen that only listed decisions already taken would show a customer nothing
/// on their first visit and imply the clinic holds no position at all.
/// </para>
/// </summary>
public sealed class GetConsentsHandler(IConsentRepository consents)
{
    public async Task<ConsentSummaryDto> HandleAsync(
        GetConsentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var current = await consents.CurrentAsync(query.CustomerId, cancellationToken);

        var states = Enum.GetValues<ConsentPurpose>()
            .Select(purpose => ConsentStateDto.From(
                purpose, current.TryGetValue(purpose, out var recorded) ? recorded : null))
            .ToList();

        var history = query.IncludeHistory
            ? (await consents.HistoryAsync(query.CustomerId, cancellationToken))
                .Select(ConsentHistoryDto.From).ToList()
            : [];

        return new ConsentSummaryDto(query.CustomerId, states, history);
    }
}
