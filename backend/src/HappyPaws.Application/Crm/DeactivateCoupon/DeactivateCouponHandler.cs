using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.DeactivateCoupon;

/// <summary>
/// <c>POST /staff/coupons/{id}/deactivate</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
public sealed record DeactivateCouponCommand(string CouponId, string? Reason = null);

public sealed class DeactivateCouponValidator : AbstractValidator<DeactivateCouponCommand>
{
    public DeactivateCouponValidator()
    {
        RuleFor(command => command.Reason).MaximumLength(500);
    }
}

/// <summary>
/// Switches a coupon off before its last day, for a code that got out or a promotion that ended
/// early.
/// <para>
/// <b>Under the coupon's lock</b>, the one every redemption takes: a cashier redeeming at the same
/// moment either finishes first, and that use stands, or waits and is refused with
/// <c>coupon_deactivated</c>. Uses already on bills are not touched; voiding such a bill still gives
/// its use back. There is no way to switch a coupon on again: a code that should work again is
/// issued anew, so the record of when it was off stays true.
/// </para>
/// <para>
/// <b>Branch.</b> The same rule as issuing: a coupon for one branch needs access to that branch, and
/// a coupon for every branch can only be switched off by someone not bound to branches. Switching
/// off twice is <c>409 state_conflict</c>.
/// </para>
/// </summary>
public sealed class DeactivateCouponHandler(
    ICouponRepository coupons,
    ICrmLocks locks,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator,
    ClinicDays clinicDays)
{
    public async Task<CouponDto> HandleAsync(DeactivateCouponCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        Coupon? coupon = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await locks.LockCouponAsync(command.CouponId, token);

            var found = await coupons.FindTrackedAsync(command.CouponId, token)
                ?? throw new NotFoundException("coupon", command.CouponId);

            if (found.BranchId is null)
            {
                if (branchGuard.VisibleBranchIds is not null)
                {
                    throw new ForbiddenException("a branch-bound staff member cannot switch off a coupon for every branch");
                }
            }
            else
            {
                branchGuard.EnsureCanAccess(found.BranchId);
            }

            found.Deactivate(staffId, command.Reason, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.CouponDeactivated,
                AuditResources.Coupon,
                found.Id,
                BranchId: found.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["code"] = found.Code,
                    ["reason"] = found.DeactivationReason,
                    ["redeemed_count"] = found.RedeemedCount,
                    ["valid_until"] = found.ValidUntil?.ToString("yyyy-MM-dd"),
                }));

            coupon = found;
        }, cancellationToken);

        return CouponDto.From(coupon!, clinicDays.Today);
    }
}
