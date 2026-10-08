using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Customers.LineAddress;

/// <summary>
/// A customer's LINE address as the counter sees it (<c>docs/contract-extensions.md § 18</c>): on
/// <c>GET /staff/customers/{id}</c> and as the answer of <c>PUT /staff/customers/{id}/line</c>.
/// The LINE user id itself is not shown; it means nothing to a person.
/// </summary>
/// <param name="CustomerId">The customer.</param>
/// <param name="Version">The customer's version, for <c>If-Match</c> on the next change.</param>
/// <param name="Linked">A LINE address is on the record.</param>
/// <param name="CanReceive">Linked and not blocked: a LINE message would reach them now.</param>
/// <param name="UnfollowedAt">When they blocked the account, while they still have.</param>
/// <param name="DisplayName">Their LINE name, while the follower list still holds it.</param>
/// <param name="PictureUrl">Their LINE picture, likewise.</param>
public sealed record CustomerLineDto(
    string CustomerId,
    uint Version,
    bool Linked,
    bool CanReceive,
    DateTimeOffset? UnfollowedAt,
    string? DisplayName,
    string? PictureUrl)
{
    public static CustomerLineDto From(Customer customer, LineFollower? follower)
    {
        var known = customer.LineUserId is not null && follower?.LineUserId == customer.LineUserId;

        return new CustomerLineDto(
            customer.Id,
            customer.Version,
            customer.LineUserId is not null,
            customer.CanReceiveLine,
            customer.LineUnfollowedAt,
            known ? follower!.DisplayName : null,
            known ? follower!.PictureUrl : null);
    }
}

/// <summary>The answers of the link that are not validation.</summary>
public static class LineAddressConflict
{
    /// <summary><c>409 line_address_taken</c>: the address is on another record, named when known.</summary>
    public static ConflictException Taken(string? customerId) =>
        new(ErrorCodes.LineAddressTaken, "LINE นี้ผูกกับลูกค้าคนอื่นอยู่แล้ว ต้องยกเลิกที่เรกคอร์ดนั้นก่อน")
        {
            MessageEn = "This LINE address is already on another customer's record.",
            Details = customerId is null ? null : new Dictionary<string, object> { ["customer_id"] = customerId },
        };
}
