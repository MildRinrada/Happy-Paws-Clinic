using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Customers;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.AdjustPoints;

/// <summary>
/// <c>POST /staff/loyalty/{customer_id}/adjustments</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
/// <param name="CustomerId">From the path.</param>
/// <param name="Points">Signed: positive gives, negative takes. Never zero.</param>
/// <param name="Reason">Why, in words someone can check later. Required.</param>
public sealed record AdjustPointsCommand(string CustomerId, int? Points = null, string? Reason = null);

public sealed class AdjustPointsValidator : AbstractValidator<AdjustPointsCommand>
{
    public const int MaxPoints = 100_000;

    public AdjustPointsValidator()
    {
        RuleFor(command => command.Points)
            .NotNull().WithMessage("ต้องระบุจำนวนแต้ม")
            .NotEqual(0).WithMessage("จำนวนแต้มต้องไม่เป็นศูนย์")
            .InclusiveBetween(-MaxPoints, MaxPoints).WithMessage($"ปรับได้ครั้งละไม่เกิน {MaxPoints:N0} แต้ม");

        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithMessage("ต้องระบุเหตุผล")
            .MaximumLength(200);
    }
}

/// <summary>
/// Gives or takes points by hand: a goodwill gesture, or points a mistake gave or withheld.
/// <para>
/// <b>One ledger row of kind <c>adjust</c></b>, signed as entered, with the reason as its note and
/// the staff member on it. Nothing else in the ledger changes. Points given expire like points
/// earned today. An adjustment moves the balance, not the lifetime points the level is judged on, so
/// a level cannot be granted by hand.
/// </para>
/// <para>
/// <b>Cannot take the balance below zero.</b> Under the customer's lock, the lock a redemption takes,
/// so a cashier spending points at the same moment and a deduction cannot both use the same points.
/// Taking more than the customer has is <c>409 insufficient_points</c>. The only way a balance goes
/// below zero stays the one there was: a refund of points already spent.
/// </para>
/// <para>
/// Clinic-wide like the account: a customer and their points are the clinic's, not a branch's.
/// Needs <c>Idempotency-Key</c>, because a retried grant must not give twice. Audited with the
/// balance before and after.
/// </para>
/// </summary>
public sealed class AdjustPointsHandler(
    ICustomerRepository customers,
    ILoyaltyLedger ledger,
    ICrmLocks locks,
    IClinicSettingsStore settings,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<LoyaltyAdjustmentDto> HandleAsync(AdjustPointsCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var customer = await customers.FindByIdAsync(command.CustomerId, cancellationToken)
            ?? throw new NotFoundException("customer", command.CustomerId);

        var points = command.Points!.Value;
        var reason = command.Reason!.Trim();

        LoyaltyEntry? entry = null;
        LoyaltyPosition? after = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;
            await locks.LockCustomerAsync(customer.Id, token);

            var before = await ledger.TotalsAsync(customer.Id, now, token);

            if (points < 0 && before.Balance + points < 0)
            {
                throw new DomainException(
                    "insufficient_points",
                    $"หักได้ไม่เกินแต้มคงเหลือ ลูกค้ามี {Math.Max(before.Balance, 0):N0} แต้ม");
            }

            var loyalty = await settings.LoyaltyAsync(token);

            entry = LoyaltyEntry.Create(
                customer.Id,
                LoyaltyEntryKind.Adjust,
                points,
                now,
                note: reason,
                staffId: staffId,
                expiresAt: loyalty.ExpiryFor(now));

            ledger.Add(entry);
            after = before with { Balance = before.Balance + points };

            audit.Record(new AuditEvent(
                AuditActions.LoyaltyAdjusted,
                AuditResources.Customer,
                customer.Id,
                Details: new Dictionary<string, object?>
                {
                    ["entry_id"] = entry.Id,
                    ["points"] = points,
                    ["reason"] = reason,
                    ["balance_before"] = before.Balance,
                    ["balance_after"] = before.Balance + points,
                    ["expires_at"] = entry.ExpiresAt,
                }));
        }, cancellationToken);

        return new LoyaltyAdjustmentDto(LoyaltyEntryDto.From(entry!, null), after!.Balance, after.Lifetime);
    }
}
