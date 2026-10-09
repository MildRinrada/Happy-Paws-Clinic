using FluentValidation;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Crm.ListCoupons;

/// <summary>
/// <c>GET /staff/coupons?status=&amp;branch=&amp;q=&amp;limit=&amp;cursor=</c>. Not in the API reference:
/// see <c>docs/contract-extensions.md § 25</c>.
/// </summary>
public sealed record ListCouponsQuery(
    string? Status = null,
    string? BranchId = null,
    string? Search = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

public sealed class ListCouponsValidator : AbstractValidator<ListCouponsQuery>
{
    public ListCouponsValidator()
    {
        // Ignoring an unknown status would answer with every coupon and look like a filtered list.
        RuleFor(query => query.Status)
            .Must(status => CrmWire.ParseCouponStatus(status) is not null)
            .WithMessage("status ต้องเป็น active, scheduled, used_up, expired หรือ deactivated")
            .When(query => !string.IsNullOrEmpty(query.Status));

        RuleFor(query => query.BranchId).MaximumLength(40);
        RuleFor(query => query.Search).MaximumLength(120);
    }
}

/// <summary>
/// The coupons issued, newest first, each with its uses and its status today.
/// <para>
/// Branch-bound staff see the coupons for their branches and those for every branch, because a bill
/// at their counter can take both. Asking for another branch is <c>403</c>. The restriction is part
/// of the query, so paging cannot pass it.
/// </para>
/// </summary>
public sealed class ListCouponsHandler(
    ICouponRepository coupons,
    BranchGuard branchGuard,
    RequestValidator validator,
    ClinicDays clinicDays)
{
    public async Task<CursorPage<CouponDto>> HandleAsync(ListCouponsQuery query, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(query, cancellationToken);

        if (query.BranchId is { Length: > 0 } requested)
        {
            branchGuard.EnsureCanAccess(requested);
        }

        var today = clinicDays.Today;
        var filter = new CouponFilter(
            today,
            CrmWire.ParseCouponStatus(query.Status),
            query.BranchId is { Length: > 0 } ? query.BranchId : null,
            query.Search?.Trim() is { Length: > 0 } search ? search : null,
            branchGuard.VisibleBranchIds);

        var page = await coupons.ListAsync(filter, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        return new CursorPage<CouponDto>([.. page.Data.Select(coupon => CouponDto.From(coupon, today))], page.NextCursor);
    }
}
