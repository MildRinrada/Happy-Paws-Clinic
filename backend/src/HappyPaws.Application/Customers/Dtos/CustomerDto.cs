using HappyPaws.Application.Customers.LineAddress;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers.Dtos;

/// <summary>A customer as search and the counter's list return them.</summary>
public sealed record CustomerDto(
    string Id,
    uint Version,
    string DisplayName,
    string Phone,
    string? Email,
    bool HasPortalAccount,
    bool IsActive,
    string? MergedIntoCustomerId,
    DateTimeOffset CreatedAt)
{
    public static CustomerDto From(Customer customer) =>
        new(
            customer.Id,
            customer.Version,
            customer.DisplayName,
            customer.Phone,
            customer.Email,
            customer.UserId is not null,
            customer.IsActive,
            customer.MergedIntoCustomerId,
            customer.CreatedAt);
}

/// <summary>
/// One customer in full — <c>GET /staff/customers/{id}</c>,
/// "ข้อมูลติดต่อ สัตว์ทั้งหมด ยอดค้างชำระ".
/// <para>
/// All three of those in one response, because all three are what the counter is looking at while
/// the person stands in front of them. Splitting the balance into a second call would mean a screen
/// that renders before it knows whether to ask for money.
/// </para>
/// <para>
/// <c>Line</c> is not in the API reference: where the customer's LINE messages go, added with the
/// counter's way of setting it (<c>docs/contract-extensions.md § 18</c>).
/// </para>
/// </summary>
public sealed record CustomerDetailDto(
    CustomerDto Customer,
    IReadOnlyList<PetSummaryDto> Pets,
    long OutstandingSatang,
    InvitationDto? Invitation,
    CustomerLineDto Line);

/// <summary>The state of a portal invitation, for the counter's "have we asked them yet".</summary>
public sealed record InvitationDto(
    string Id,
    string SentTo,
    string InvitedByStaffId,
    DateTimeOffset SentAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? AcceptedAt,
    bool IsLive)
{
    public static InvitationDto From(CustomerInvitation invitation, DateTimeOffset now) =>
        new(
            invitation.Id,
            invitation.SentTo,
            invitation.InvitedByStaffId,
            invitation.SentAt,
            invitation.ExpiresAt,
            invitation.AcceptedAt,
            invitation.IsLiveAt(now));
}

/// <summary>What a merge moved, per table, so the audit row and the response say the same thing.</summary>
public sealed record MergeResultDto(
    CustomerDto Surviving,
    CustomerDto Merged,
    IReadOnlyDictionary<string, int> Moved);
