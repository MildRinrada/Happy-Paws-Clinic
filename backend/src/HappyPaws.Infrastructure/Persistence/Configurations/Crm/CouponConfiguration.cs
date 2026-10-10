using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Crm;
using HappyPaws.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Crm;

internal sealed class CouponConfiguration : EntityConfiguration<Coupon>
{
    /// <summary>Named so a race between two clerks choosing the same code reads as <c>coupon_code_taken</c>.</summary>
    public const string CodeIndex = "ux_coupons_code";

    protected override void ConfigureEntity(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        builder.Property(coupon => coupon.Code).HasMaxLength(20).IsRequired();
        builder.Property(coupon => coupon.Name).HasMaxLength(120).IsRequired();
        builder.Property(coupon => coupon.BranchId).HasMaxLength(40);
        builder.Property(coupon => coupon.CreatedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(coupon => coupon.DeactivatedByStaffId).HasMaxLength(40);
        builder.Property(coupon => coupon.DeactivationReason).HasMaxLength(500);

        builder.PrimitiveCollection(coupon => coupon.ServiceIds)
            .HasField("_serviceIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(coupon => coupon.Code).IsUnique().HasDatabaseName(CodeIndex);

        // GET /staff/coupons pages newest first (docs/contract-extensions.md § 25).
        builder.HasIndex(coupon => new { coupon.CreatedAt, coupon.Id });

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_coupons_one_kind", "(percent IS NULL) <> (amount_satang IS NULL)");
            table.HasCheckConstraint(
                "ck_coupons_redeemed",
                "redeemed_count >= 0 AND (max_redemptions IS NULL OR redeemed_count <= max_redemptions)");
            table.HasCheckConstraint(
                "ck_coupons_deactivated_by",
                "(deactivated_at IS NULL) = (deactivated_by_staff_id IS NULL)");
        });

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(coupon => coupon.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CouponRedemptionConfiguration : EntityConfiguration<CouponRedemption>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("coupon_redemptions");

        builder.Property(redemption => redemption.CouponId).HasMaxLength(40).IsRequired();
        builder.Property(redemption => redemption.Code).HasMaxLength(20).IsRequired();
        builder.Property(redemption => redemption.InvoiceId).HasMaxLength(40).IsRequired();
        builder.Property(redemption => redemption.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(redemption => redemption.DiscountId).HasMaxLength(40).IsRequired();
        builder.Property(redemption => redemption.RedeemedByStaffId).HasMaxLength(40).IsRequired();

        // One standing use of a coupon per bill. A voided bill's row is released and no longer counts.
        builder.HasIndex(redemption => new { redemption.CouponId, redemption.InvoiceId })
            .IsUnique()
            .HasFilter("released_at IS NULL");

        builder.HasIndex(redemption => new { redemption.CouponId, redemption.CustomerId });
        builder.HasIndex(redemption => redemption.InvoiceId);

        builder.HasOne<Coupon>().WithMany()
            .HasForeignKey(redemption => redemption.CouponId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany()
            .HasForeignKey(redemption => redemption.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(redemption => redemption.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LoyaltyEntryConfiguration : EntityConfiguration<LoyaltyEntry>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LoyaltyEntry> builder)
    {
        builder.ToTable("loyalty_entries");

        builder.Property(entry => entry.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(entry => entry.Kind).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(entry => entry.InvoiceId).HasMaxLength(40);
        builder.Property(entry => entry.DiscountId).HasMaxLength(40);
        builder.Property(entry => entry.Note).HasMaxLength(200);
        builder.Property(entry => entry.StaffId).HasMaxLength(40);

        // A bill earns once. The payment path checks first; this is the backstop.
        builder.HasIndex(entry => entry.InvoiceId)
            .IsUnique()
            .HasFilter("kind = 'Earn'")
            .HasDatabaseName("ux_loyalty_entries_earn_per_invoice");

        // A referral pays each person once (docs/contract-extensions.md § 39). The rewarder closes
        // the referral under the friend's lock first; this is the backstop.
        builder.Property(entry => entry.ReferralId).HasMaxLength(40);
        builder.HasIndex(entry => new { entry.ReferralId, entry.CustomerId })
            .IsUnique()
            .HasFilter("kind = 'Referral'")
            .HasDatabaseName("ux_loyalty_entries_referral_per_customer");

        builder.HasIndex(entry => new { entry.CustomerId, entry.OccurredAt });

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(entry => entry.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany()
            .HasForeignKey(entry => entry.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// The points programme (<c>docs/contract-extensions.md § 25</c>): one row, like the clinic's other
/// settings, and levels that rise.
/// </summary>
internal sealed class LoyaltySettingsConfiguration : EntityConfiguration<LoyaltySettings>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LoyaltySettings> builder)
    {
        builder.ToTable("loyalty_settings", table =>
        {
            table.HasCheckConstraint("ck_loyalty_settings_singleton", $"id = '{LoyaltySettings.SingletonId}'");
            table.HasCheckConstraint(
                "ck_loyalty_settings_values",
                "satang_per_point > 0 AND point_value_satang > 0 AND silver_min_points > 0 AND gold_min_points > silver_min_points "
                + "AND (points_expire_after_months IS NULL OR points_expire_after_months > 0)");
            table.HasCheckConstraint(
                "ck_loyalty_settings_referral",
                "referral_referrer_points >= 0 AND referral_friend_points >= 0 AND referral_monthly_cap > 0");
        });

        // Refer a friend (docs/contract-extensions.md § 39). The defaults put a saved row from before
        // this migration where an unsaved one starts: switched off, 100 and 100 points, five a month.
        // The sentinel is a value the entity never holds, so EF always writes the real value and a
        // clinic that saves 0 points gets 0, not the column's default.
        builder.Property(settings => settings.ReferralEnabled).HasDefaultValue(false);
        builder.Property(settings => settings.ReferralReferrerPoints).HasDefaultValue(LoyaltySettings.DefaultReferralPoints).HasSentinel(-1);
        builder.Property(settings => settings.ReferralFriendPoints).HasDefaultValue(LoyaltySettings.DefaultReferralPoints).HasSentinel(-1);
        builder.Property(settings => settings.ReferralMonthlyCap).HasDefaultValue(LoyaltySettings.DefaultReferralMonthlyCap).HasSentinel(-1);
    }
}

internal sealed class SegmentConfiguration : EntityConfiguration<Segment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Segment> builder)
    {
        builder.ToTable("customer_segments");

        builder.Property(segment => segment.Name).HasMaxLength(120).IsRequired();
        builder.Property(segment => segment.BranchId).HasMaxLength(40);
        builder.Property(segment => segment.CreatedByStaffId).HasMaxLength(40).IsRequired();

        builder.PrimitiveCollection(segment => segment.Species)
            .HasField("_species")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.PrimitiveCollection(segment => segment.Tiers)
            .HasField("_tiers")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .ElementType(element => element.HasConversion<string>());

        builder.Ignore(segment => segment.Criteria);

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(segment => segment.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
