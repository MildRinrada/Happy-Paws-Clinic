using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.CreateCoupon;

/// <summary><c>POST /staff/coupons</c> — "โค้ดส่วนลด · จำกัดบริการ จำนวน และช่วงเวลา".</summary>
public sealed record CreateCouponCommand(
    string Name,
    string? Code = null,
    decimal? Percent = null,
    long? AmountSatang = null,
    IReadOnlyList<string>? ServiceIds = null,
    int? MaxRedemptions = null,
    int? PerCustomerLimit = null,
    DateOnly? ValidFrom = null,
    DateOnly? ValidUntil = null,
    string? BranchId = null);

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage("ต้องระบุชื่อคูปอง").MaximumLength(120);

        RuleFor(command => command.Code)
            .Must(code => Coupon.IsWellFormedCode(Coupon.NormalizeCode(code)))
            .WithMessage("โค้ดต้องเป็นตัวอักษรอังกฤษ ตัวเลข หรือขีด 4 ถึง 20 ตัว")
            .When(command => command.Code is not null);

        RuleFor(command => command)
            .Must(command => command.Percent.HasValue ^ command.AmountSatang.HasValue)
            .WithMessage("ระบุ percent หรือ amount_satang อย่างใดอย่างหนึ่ง");

        RuleFor(command => command.Percent)
            .GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("เปอร์เซ็นต์ต้องมากกว่า 0 และไม่เกิน 100")
            .Must(percent => percent is null || decimal.Round(percent.Value, 2) == percent.Value)
            .WithMessage("เปอร์เซ็นต์มีทศนิยมได้ไม่เกินสองตำแหน่ง")
            .When(command => command.Percent.HasValue);

        RuleFor(command => command.AmountSatang)
            .GreaterThan(0).WithMessage("ส่วนลดต้องมากกว่าศูนย์")
            .When(command => command.AmountSatang.HasValue);

        RuleFor(command => command.ServiceIds)
            .Must(ids => ids is null || ids.Count <= Coupon.MaxServices)
            .WithMessage($"เลือกบริการได้ไม่เกิน {Coupon.MaxServices} บริการ");
        RuleForEach(command => command.ServiceIds).NotEmpty().MaximumLength(40);

        RuleFor(command => command.MaxRedemptions).InclusiveBetween(1, 100_000).When(command => command.MaxRedemptions.HasValue);
        RuleFor(command => command.PerCustomerLimit).InclusiveBetween(1, 100).When(command => command.PerCustomerLimit.HasValue);

        RuleFor(command => command)
            .Must(command => command.ValidUntil is null || command.ValidFrom is null || command.ValidUntil >= command.ValidFrom)
            .OverridePropertyName("valid_until")
            .WithMessage("วันสิ้นสุดต้องไม่ก่อนวันเริ่ม");
    }
}

/// <summary>
/// Issues a discount code.
/// <para>
/// <b>Branch.</b> A coupon for one branch needs access to that branch. A coupon for every branch
/// can only be issued by someone who is not bound to branches (an owner or admin): a counter at one
/// branch should not be able to give away money at the others.
/// </para>
/// </summary>
public sealed class CreateCouponHandler(
    ICouponRepository coupons,
    IBookingCatalog catalog,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    ClinicDays clinicDays)
{
    public async Task<CouponDto> HandleAsync(
        CreateCouponCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var today = clinicDays.Today;

        if (command.ValidUntil is { } until && until < today)
        {
            throw Invalid("valid_until", "วันสิ้นสุดต้องไม่ก่อนวันนี้");
        }

        var branchId = await CrmBranches.ResolveOptionalAsync(catalog, command.BranchId, cancellationToken);

        if (branchId is null)
        {
            if (branchGuard.VisibleBranchIds is not null)
            {
                throw new ForbiddenException("a branch-bound staff member cannot issue a coupon for every branch");
            }
        }
        else
        {
            branchGuard.EnsureCanAccess(branchId);
        }

        var serviceIds = (command.ServiceIds ?? []).Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToList();
        await CrmServices.EnsureExistAsync(catalog, serviceIds, "service_ids", cancellationToken);

        Coupon? coupon = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var code = await ChooseCodeAsync(command.Code, token);

            coupon = Coupon.Create(
                code,
                command.Name,
                command.Percent,
                command.AmountSatang,
                serviceIds,
                command.MaxRedemptions,
                command.PerCustomerLimit,
                command.ValidFrom ?? today,
                command.ValidUntil,
                branchId,
                staffId);

            coupons.Add(coupon);

            audit.Record(new AuditEvent(
                AuditActions.CouponCreated,
                AuditResources.Coupon,
                coupon.Id,
                BranchId: branchId,
                Details: new Dictionary<string, object?>
                {
                    ["code"] = coupon.Code,
                    ["percent"] = coupon.Percent,
                    ["amount_satang"] = coupon.AmountSatang,
                    ["service_ids"] = coupon.ServiceIds,
                    ["max_redemptions"] = coupon.MaxRedemptions,
                    ["per_customer_limit"] = coupon.PerCustomerLimit,
                    ["valid_from"] = coupon.ValidFrom.ToString("yyyy-MM-dd"),
                    ["valid_until"] = coupon.ValidUntil?.ToString("yyyy-MM-dd"),
                }));
        }, cancellationToken);

        return CouponDto.From(coupon!);
    }

    /// <summary>
    /// The code asked for, refused if taken; otherwise a generated one. The unique index on the
    /// code settles a race between two clerks typing the same code at once.
    /// </summary>
    private async Task<string> ChooseCodeAsync(string? requested, CancellationToken cancellationToken)
    {
        if (requested is not null)
        {
            var code = Coupon.NormalizeCode(requested);

            if (await coupons.CodeExistsAsync(code, cancellationToken))
            {
                throw new ConflictException(ErrorCodes.CouponCodeTaken, $"มีคูปองโค้ด {code} อยู่แล้ว")
                {
                    MessageEn = "A coupon with this code already exists.",
                };
            }

            return code;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = Coupon.GenerateCode();

            if (!await coupons.CodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not generate a free coupon code.");
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
