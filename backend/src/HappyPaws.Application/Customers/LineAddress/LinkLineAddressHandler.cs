using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Customers.LineAddress;

/// <summary>
/// <c>PUT /staff/customers/{id}/line</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 18</c>.
/// </summary>
/// <param name="CustomerId">The customer at the counter.</param>
/// <param name="LineUserId">An address from <c>GET /staff/integrations/line/followers</c>.</param>
/// <param name="IfMatchVersion">The customer's version, from <c>If-Match</c>.</param>
public sealed record LinkLineAddressCommand(string CustomerId, string? LineUserId, uint? IfMatchVersion = null);

public sealed class LinkLineAddressValidator : AbstractValidator<LinkLineAddressCommand>
{
    public LinkLineAddressValidator()
    {
        RuleFor(command => command.LineUserId)
            .NotEmpty().WithMessage("ต้องระบุ line_user_id จากรายชื่อผู้ติดตาม")
            .MaximumLength(LineFollower.LineUserIdMax).WithMessage("line_user_id ยาวเกินไป");
    }
}

/// <summary>
/// Saves the LINE address the counter picked as the customer's, as it would an email address.
/// <para>
/// <b>Only an address LINE has shown us.</b> The id must be on the follower list: followed or wrote
/// in the last <see cref="LineFollower.Retention"/>, and still following. An id from anywhere else
/// is refused, because nothing could check it is a real follower of this account, and an unknown one
/// would only turn every message to this customer into a failed push.
/// </para>
/// <para>
/// <b>One record per address.</b> An address on another customer is <c>409 line_address_taken</c>
/// naming that record; the counter unlinks it there first. Saving the address the record already
/// has changes nothing and writes no audit row. A different address replaces the old one.
/// </para>
/// <para>
/// Linking sends the customer nothing and proves nothing about who they are. If they later block
/// the account, the webhook marks the record as it does any other, and messages go by SMS.
/// </para>
/// </summary>
public sealed class LinkLineAddressHandler(
    ICustomerRepository customers,
    ILineFollowerRepository followers,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<CustomerLineDto> HandleAsync(LinkLineAddressCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var lineUserId = command.LineUserId!.Trim();
        Customer? linked = null;
        LineFollower? follower = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var customer = await customers.FindTrackedByIdAsync(command.CustomerId, token)
                ?? throw new NotFoundException("customer", command.CustomerId);

            ConcurrencyGuard.Ensure(customer, command.IfMatchVersion);
            follower = await followers.FindAsync(lineUserId, token);
            linked = customer;

            if (customer.LineUserId == lineUserId)
            {
                return;
            }

            if (customer.IsMerged)
            {
                throw new StateConflictException(nameof(Customer), "merged", "link_line");
            }

            if (await customers.WithLineAddressAsync(lineUserId, token) is { } holder)
            {
                throw LineAddressConflict.Taken(holder.Id);
            }

            if (follower is null || follower.LastSeenAt < clock.UtcNow - LineFollower.Retention)
            {
                throw Invalid("ไม่พบผู้ติดตามนี้ในรายชื่อ 14 วันล่าสุด ให้ลูกค้าเพิ่มเพื่อนหรือพิมพ์ข้อความถึงบัญชี LINE ของคลินิก แล้วโหลดรายชื่อใหม่");
            }

            if (!follower.IsFollowing)
            {
                throw Invalid("ผู้ติดตามคนนี้บล็อกบัญชี LINE ของคลินิกแล้ว ข้อความจะส่งไม่ถึง");
            }

            var replaced = customer.LineUserId is not null;
            customer.SetLineAddress(lineUserId);

            audit.Record(new AuditEvent(
                AuditActions.CustomerLineLinked,
                AuditResources.Customer,
                customer.Id,
                Details: new Dictionary<string, object?>
                {
                    ["line_user_id"] = lineUserId,
                    ["replaced"] = replaced,
                }));
        }, cancellationToken);

        return CustomerLineDto.From(linked!, follower);
    }

    private static ValidationFailedException Invalid(string message) =>
        new(new Dictionary<string, string[]> { ["line_user_id"] = [message] });
}
