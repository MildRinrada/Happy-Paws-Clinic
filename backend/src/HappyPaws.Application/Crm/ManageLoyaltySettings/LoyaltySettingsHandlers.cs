using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.ManageLoyaltySettings;

/// <summary>
/// <c>GET /clinic/settings/loyalty</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
public sealed class GetLoyaltySettingsHandler(IClinicSettingsStore settings)
{
    public async Task<LoyaltySettingsDto> HandleAsync(CancellationToken cancellationToken = default) =>
        LoyaltySettingsDto.From(await settings.LoyaltyAsync(cancellationToken));
}

/// <summary>
/// <c>PUT /clinic/settings/loyalty</c> (§ 25). A <c>PUT</c>: every value is sent. Absent
/// <c>points_expire_after_months</c> means points do not expire.
/// </summary>
public sealed record ReplaceLoyaltySettingsCommand(
    long? SatangPerPoint = null,
    long? PointValueSatang = null,
    int? SilverMinPoints = null,
    int? GoldMinPoints = null,
    int? PointsExpireAfterMonths = null,
    uint? IfMatchVersion = null);

public sealed class ReplaceLoyaltySettingsValidator : AbstractValidator<ReplaceLoyaltySettingsCommand>
{
    public ReplaceLoyaltySettingsValidator()
    {
        // The entity's bounds, stated here so a bad value answers 422 and names its field.
        RuleFor(command => command.SatangPerPoint)
            .NotNull().WithMessage("ต้องระบุยอดชำระต่อหนึ่งแต้ม")
            .InclusiveBetween(LoyaltySettings.MinSatangPerPoint, LoyaltySettings.MaxSatangPerPoint)
            .WithMessage("ยอดชำระต่อหนึ่งแต้มต้องอยู่ระหว่าง ฿1 ถึง ฿100,000");

        RuleFor(command => command.PointValueSatang)
            .NotNull().WithMessage("ต้องระบุมูลค่าแต้ม")
            .InclusiveBetween(1, LoyaltySettings.MaxPointValueSatang)
            .WithMessage("มูลค่าแต้มต้องอยู่ระหว่าง 1 สตางค์ ถึง ฿1,000");

        RuleFor(command => command.SilverMinPoints)
            .NotNull().WithMessage("ต้องระบุเกณฑ์ระดับ silver")
            .InclusiveBetween(1, LoyaltySettings.MaxTierPoints - 1)
            .WithMessage("เกณฑ์ระดับ silver ต้องอย่างน้อย 1 แต้ม");

        RuleFor(command => command.GoldMinPoints)
            .NotNull().WithMessage("ต้องระบุเกณฑ์ระดับ gold")
            .InclusiveBetween(2, LoyaltySettings.MaxTierPoints)
            .WithMessage("เกณฑ์ระดับ gold ไม่ถูกต้อง")
            .GreaterThan(command => command.SilverMinPoints ?? 0)
            .WithMessage("เกณฑ์ระดับ gold ต้องมากกว่า silver");

        RuleFor(command => command.PointsExpireAfterMonths)
            .InclusiveBetween(1, LoyaltySettings.MaxExpiryMonths)
            .WithMessage($"อายุแต้มต้องอยู่ระหว่าง 1 ถึง {LoyaltySettings.MaxExpiryMonths} เดือน")
            .When(command => command.PointsExpireAfterMonths is not null);
    }
}

/// <summary>
/// Replaces the points programme.
/// <para>
/// <b>From now on, never backwards.</b> Bills paid from now earn at the new rate; points redeemed
/// from now are worth the new value; points given from now carry the new expiry. Ledger rows already
/// written keep their points, their expiry and, on earn rows, the rate they were earned at, which a
/// later refund uses. Levels are worked out on every read, so new thresholds re-label customers at
/// once; the points behind them do not move. Turning expiry off does not revive points already
/// stamped to expire, and turning it on does not stamp points already given.
/// </para>
/// <para>
/// <c>If-Match</c> as the clinic's other settings: a stale version is <c>409 version_conflict</c>.
/// Audited with the before and after, because the value of a point is money.
/// </para>
/// </summary>
public sealed class ReplaceLoyaltySettingsHandler(
    IClinicSettingsStore settings,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<LoyaltySettingsDto> HandleAsync(
        ReplaceLoyaltySettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        LoyaltySettings? saved = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var loyalty = await settings.LoyaltyForUpdateAsync(token);
            ConcurrencyGuard.Ensure(loyalty, command.IfMatchVersion);

            var before = Values(loyalty);

            loyalty.Replace(
                command.SatangPerPoint!.Value,
                command.PointValueSatang!.Value,
                command.SilverMinPoints!.Value,
                command.GoldMinPoints!.Value,
                command.PointsExpireAfterMonths);

            audit.Record(new AuditEvent(
                AuditActions.LoyaltySettingsChanged,
                AuditResources.Settings,
                loyalty.Id,
                Details: new Dictionary<string, object?>
                {
                    ["before"] = before,
                    ["after"] = Values(loyalty),
                }));

            saved = loyalty;
        }, cancellationToken);

        return LoyaltySettingsDto.From(saved!);
    }

    private static Dictionary<string, object?> Values(LoyaltySettings loyalty) => new()
    {
        ["satang_per_point"] = loyalty.SatangPerPoint,
        ["point_value_satang"] = loyalty.PointValueSatang,
        ["silver_min_points"] = loyalty.SilverMinPoints,
        ["gold_min_points"] = loyalty.GoldMinPoints,
        ["points_expire_after_months"] = loyalty.PointsExpireAfterMonths,
    };
}
