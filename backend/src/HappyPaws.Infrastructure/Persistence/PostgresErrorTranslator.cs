using HappyPaws.Application.Articles.Manage;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Infrastructure.Persistence.Configurations.Articles;
using HappyPaws.Application.Customers.LineAddress;
using HappyPaws.Infrastructure.Persistence.Configurations.Crm;
using HappyPaws.Infrastructure.Persistence.Configurations.Customers;
using HappyPaws.Application.Inventory;
using HappyPaws.Infrastructure.Persistence.Configurations.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HappyPaws.Infrastructure.Persistence;

/// <summary>
/// Turns the database constraints that back a contract rule into the contract's own errors.
/// <para>
/// Some invariants can only be enforced where every writer meets — the appointment exclusion
/// constraint is the clear case, because two concurrent bookings cannot see each other's
/// uncommitted rows however carefully the use case checks. When the database refuses such a write,
/// the caller deserves the error the contract defines for it, not a generic 500 that reads like an
/// outage.
/// </para>
/// <para>
/// Deliberately narrow. Only constraints this system creates on purpose are translated, matched by
/// name; anything else stays an unexpected fault, because a violation nobody planned for is a bug
/// and should look like one.
/// </para>
/// </summary>
internal static class PostgresErrorTranslator
{
    /// <summary>PostgreSQL <c>exclusion_violation</c>.</summary>
    private const string ExclusionViolation = "23P01";

    private const string AppointmentSlotConstraint = "ex_appointments_staff_slot";

    /// <summary>PostgreSQL <c>unique_violation</c>.</summary>
    private const string UniqueViolation = "23505";

    /// <summary>
    /// The contract error for this failure, or null when the failure is not one we planned for.
    /// </summary>
    public static Exception? Translate(DbUpdateException exception) =>
        exception.InnerException switch
        {
            // No suggestions: the losing request already passed its own availability check, so
            // anything computed now would be from a view of the calendar that has just changed
            // underneath it. An honest empty list beats a stale one.
            PostgresException { SqlState: ExclusionViolation, ConstraintName: AppointmentSlotConstraint } =>
                SlotUnavailableException.AlreadyBooked([]),

            // Two people adding the same new drug at the same moment: the loser is told it exists,
            // the same answer it would have had a second later.
            PostgresException { SqlState: UniqueViolation, ConstraintName: InventoryItemConfiguration.NameKeyIndex } =>
                InventoryItemResolver.Exists(existingId: null),

            // Two counters linking one LINE address to two customers at the same moment
            // (docs/contract-extensions.md § 18): the loser hears what it would have a second later.
            PostgresException { SqlState: UniqueViolation, ConstraintName: CustomerConfiguration.LineAddressIndex } =>
                LineAddressConflict.Taken(customerId: null),

            // Two clerks choosing the same coupon code at the same moment: the loser hears what it
            // would have a second later.
            PostgresException { SqlState: UniqueViolation, ConstraintName: CouponConfiguration.CodeIndex } =>
                new ConflictException(ErrorCodes.CouponCodeTaken, "มีคูปองโค้ดนี้อยู่แล้ว")
                {
                    MessageEn = "A coupon with this code already exists.",
                },

            // Two writers taking the same slug at the same moment (docs/contract-extensions.md § 26):
            // the loser hears what it would have a second later, without the holder it cannot see.
            PostgresException { SqlState: UniqueViolation, ConstraintName: ArticleConfiguration.SlugIndex } =>
                ArticleSlugs.Taken(slug: null, holder: null),

            _ => null,
        };
}
